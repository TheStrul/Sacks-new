namespace Sacks.LogicLayer.Services
{
    using Microsoft.Extensions.Logging;
    using System.Text.Json;
    using Sacks.Core.FileProcessing.Configuration;
    using Sacks.Core.Configuration;
    using System.Threading.Tasks;
    using Sacks.Configuration;

    /// <summary>
    /// Manages loading and updating supplier configurations from JSON files
    /// /// </summary>
    public class SupplierConfigurationManager
    {
        private readonly ConfigurationFilesOptions _configOptions;
        private SuppliersConfiguration? _suppliersConfiguration = null;
        private readonly ILogger<SupplierConfigurationManager> _logger;
        private readonly JsonSupplierConfigurationLoader loader;

        private FileSystemWatcher? _watcher;
        private readonly object _sync = new();
        private DateTime _lastReload = DateTime.MinValue;
        private string? _mainConfigFullPath;

        /// <summary>
        /// Raised when configuration is reloaded from disk.
        /// </summary>
        public event EventHandler? ConfigurationReloaded;

        /// <summary>
        /// Creates a new instance using JsonSupplierConfigurationLoader to build SuppliersConfiguration
        /// /// </summary>
        public SupplierConfigurationManager(ConfigurationFilesOptions configOptions, ILogger<SupplierConfigurationManager> logger)
        {
            ArgumentNullException.ThrowIfNull(configOptions);
            ArgumentNullException.ThrowIfNull(logger);

            _logger = logger;
            _configOptions = configOptions;

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

                                // SetAssign parent reference before returning
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

            var baseFolder = AppContext.BaseDirectory;
            string fileNmae = Path.Combine(_configOptions.ConfigurationFolder, _configOptions.MainFileName);
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
                throw new FileNotFoundException($"Could not locate configuration folder '{_configOptions.ConfigurationFolder}' with required main file '{_configOptions.MainFileName}' starting from '{baseFolder}'");
            }

            // Load configuration from loader (supports file or directory)
            var loaded = await loader.LoadAllFromFolderAsync(foundPath);
            _suppliersConfiguration = loaded;
            _mainConfigFullPath = foundPath;
            TryStartWatcher(foundPath);
        }

        private void TryStartWatcher(string mainFilePath)
        {
            try
            {
                var dir = Path.GetDirectoryName(mainFilePath)!;
                var mainFile = Path.GetFileName(mainFilePath);
                _watcher = new FileSystemWatcher(dir, "*.json")
                {
                    IncludeSubdirectories = false,
                    EnableRaisingEvents = true,
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size
                };

                _watcher.Changed += OnJsonChanged;
                _watcher.Created += OnJsonChanged;
                _watcher.Deleted += OnJsonChanged;
                _watcher.Renamed += OnJsonRenamed;

                _logger.LogInformation("Supplier configuration watcher started on {Dir}", dir);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start configuration file watcher");
            }
        }

        private void OnJsonRenamed(object sender, RenamedEventArgs e)
        {
            DebouncedReload(e.FullPath);
        }

        private void OnJsonChanged(object sender, FileSystemEventArgs e)
        {
            DebouncedReload(e.FullPath);
        }

        private void DebouncedReload(string changedPath)
        {
            // debounce bursts
            lock (_sync)
            {
                var now = DateTime.UtcNow;
                if ((now - _lastReload).TotalMilliseconds < 250)
                {
                    return;
                }
                _lastReload = now;
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    // wait a little so writers close the file
                    await Task.Delay(300);
                    if (_mainConfigFullPath == null) return;

                    var newConfig = await loader.LoadAllFromFolderAsync(_mainConfigFullPath);

                    lock (_sync)
                    {
                        if (_suppliersConfiguration == null)
                        {
                            _suppliersConfiguration = newConfig;
                        }
                        else
                        {
                            _suppliersConfiguration.ApplyFrom(newConfig);
                        }
                    }

                    _logger.LogInformation("Supplier configuration reloaded from disk due to change: {Path}", changedPath);
                    ConfigurationReloaded?.Invoke(this, EventArgs.Empty);
                }
                catch (JsonException jex)
                {
                    _logger.LogError(jex, "Invalid JSON while reloading supplier configuration. Keeping previous version.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to reload supplier configuration. Keeping previous version.");
                }
            });
        }
    }
}
