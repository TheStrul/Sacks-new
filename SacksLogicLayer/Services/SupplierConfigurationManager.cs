namespace SacksLogicLayer.Services
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;

    using SacksDataLayer.FileProcessing.Configuration;
    using SacksDataLayer.Configuration;
    using System.Threading.Tasks;

    /// <summary>
    /// Manages loading and updating supplier configurations from JSON files
    /// /// </summary>
    public class SupplierConfigurationManager
    {
        private IConfiguration _configuration;
        private ISuppliersConfiguration? _suppliersConfiguration = null;
        private readonly ILogger<SupplierConfigurationManager> _logger;
        private readonly JsonSupplierConfigurationLoader loader;

        /// <summary>
        /// Creates a new instance using JsonSupplierConfigurationLoader to build SuppliersConfiguration
        /// /// </summary>
        public SupplierConfigurationManager(IConfiguration configuration, ILogger<SupplierConfigurationManager> logger)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            ArgumentNullException.ThrowIfNull(logger);

            _logger = logger;
            _configuration = configuration;


            loader = new JsonSupplierConfigurationLoader(_logger);

        }

        /// <summary>
        /// Gets all supplier configurations
        /// /// </summary>
        public async Task<ISuppliersConfiguration> GetConfigurationAsync()
        {
            await EnsureConfigurationLoadedAsync();
            return this._suppliersConfiguration!;
        }


        /// <summary>
        /// Auto-detects supplier configuration from file path/name
        /// /// </summary>
        public SupplierConfiguration? DetectSupplierFromFileAsync(string filePath)
        {

            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            try
            {

                var fileName = Path.GetFileName(filePath);
                var fileNameLowerCase = fileName.ToLowerInvariant(); // Convert to lowercase for matching
                _logger?.LogDebug("Detecting supplier configuration for file: {FileName} (matching as: {FileNameLower})", fileName, fileNameLowerCase);

                // Try to match against each supplier's file name patterns using lowercase filename
                foreach (var supplier in _suppliersConfiguration!.Suppliers)
                {
                    if (supplier.FileStructure.Detection?.FileNamePatterns?.Any() == true)
                    {
                        foreach (var pattern in supplier.FileStructure.Detection.FileNamePatterns)
                        {
                            // Convert pattern to lowercase for case-insensitive matching
                            var patternLowerCase = pattern.ToLowerInvariant();
                            if (IsFileNameMatch(fileNameLowerCase, patternLowerCase))
                            {
                                _logger?.LogDebug("Matched supplier '{SupplierName}' with pattern '{Pattern}' (as '{PatternLower}') for file '{FileName}'",
                                    supplier.Name, pattern, patternLowerCase, fileName);

                                // Set parent reference before returning
                                supplier.ParentConfiguration = _suppliersConfiguration;
                                return supplier;
                            }
                        }
                    }
                }

                _logger?.LogWarning("No supplier configuration found for file: {FileName}", fileName);
                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error detecting supplier configuration for file: {FilePath}", filePath);
                return null;
            }
        }

        /// <summary>
        /// Checks if a file name matches a pattern (supports wildcards)
        /// Note: Both fileName and pattern should already be normalized to the same case
        /// /// </summary>
        private bool IsFileNameMatch(string fileName, string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern) || string.IsNullOrWhiteSpace(fileName))
                return false;

            // Convert pattern to regex
            var regexPattern = pattern
                .Replace("*", ".*")
                .Replace("?", ".");

            return System.Text.RegularExpressions.Regex.IsMatch(fileName, regexPattern);
        }


        private async Task EnsureConfigurationLoadedAsync()
        {
            if (_suppliersConfiguration != null)
            {
                return;
            }
            // Read configuration files paths from appsettings
            var configFiles = _configuration.GetSection("ConfigurationFiles").Get<ConfigurationFileSettings>();
            if (configFiles == null)
            {
                throw new InvalidOperationException("ConfigurationFiles section missing in appsettings.json");
            }

            var baseFolder = AppContext.BaseDirectory;
            string fileNmae = Path.Combine(configFiles.ConfigurationFolder, configFiles.MainFileName);
            string? foundPath = null;

            // climb a few levels to locate the configuration folder
            for (int i = 0; i < 6; i++)
            {
                string fullPath = Path.Combine(baseFolder, fileNmae);

                if (File.Exists(fullPath))
                {
                    foundPath = fullPath;
                    break;
                }
                var dir = Directory.GetParent(baseFolder);
                if (dir == null) break;
                baseFolder = dir.FullName;
            }

            if (foundPath == null)
            {
                throw new FileNotFoundException($"Could not locate configuration folder '{configFiles.ConfigurationFolder}' with required main file '{configFiles.MainFileName}' starting from '{baseFolder}'");
            }

            // Load configuration from loader (supports file or directory)
            _suppliersConfiguration = await loader.LoadAllFromFolderAsync(foundPath);
        }
    }
}