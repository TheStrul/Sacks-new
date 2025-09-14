namespace SacksLogicLayer.Services
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;

    using SacksDataLayer.FileProcessing.Configuration;

    /// <summary>
    /// Manages loading and updating supplier configurations from JSON files
    /// /// </summary>
    public class SupplierConfigurationManager
    {
        private SuppliersConfiguration? _configuration;
        private readonly ILogger<SupplierConfigurationManager>? _logger;

        /// <summary>
        /// Creates a new instance with IConfiguration support (recommended)
        /// /// </summary>
        public SupplierConfigurationManager(IConfiguration configuration, ILogger<SupplierConfigurationManager>? logger = null)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            // Try to get from root level first (loaded from supplier-formats.json)
            _configuration = configuration.Get<SuppliersConfiguration>();
            
            // If that fails, try the "SupplierFormats" section
            _configuration ??= configuration.GetSection("SupplierFormats").Get<SuppliersConfiguration>();
            
            if (_configuration == null)
            {
                throw new InvalidOperationException("Failed to bind SupplierFormats configuration. Ensure supplier-formats.json is loaded correctly.");
            }

            _logger = logger;
        }

        /// <summary>
        /// Gets all supplier configurations
        /// /// </summary>
        public async Task<SuppliersConfiguration> GetConfigurationAsync()
        {
            await EnsureConfigurationLoadedAsync();
            return _configuration!;
        }


        /// <summary>
        /// Gets all supplier configurations ordered by name
        /// /// </summary>
        public async Task<List<SupplierConfiguration>> GetSupplierConfigurationsByPriorityAsync()
        {
            var config = await GetConfigurationAsync();
            var suppliers = config.Suppliers
                .OrderBy(s => s.Name)
                .ToList();
            
            // Ensure parent references are set
            foreach (var supplier in suppliers)
            {
                supplier.ParentConfiguration = config;
            }
            
            return suppliers;
        }

        /// <summary>
        /// Adds a new supplier configuration
        /// /// </summary>
        public async Task AddSupplierConfigurationAsync(SupplierConfiguration supplierConfig)
        {
            ArgumentNullException.ThrowIfNull(supplierConfig);
            
            var config = await GetConfigurationAsync();
            
            // Remove existing configuration with same name
            config.Suppliers.RemoveAll(s => 
                string.Equals(s.Name, supplierConfig.Name, StringComparison.OrdinalIgnoreCase));
            
            // Set parent reference and add new configuration
            supplierConfig.ParentConfiguration = config;
            config.Suppliers.Add(supplierConfig);
            
            await SaveConfigurationAsync(config);
        }



        /// <summary>
        /// Reloads configuration from file
        /// /// </summary>
        public async Task ReloadConfigurationAsync()
        {
            await EnsureConfigurationLoadedAsync();
        }

        /// <summary>
        /// Validates the configuration file
        /// /// </summary>
        public async Task<ConfigurationValidationResult> ValidateConfigurationAsync()
        {
            var result = new ConfigurationValidationResult();
            
            try
            {
                var config = await GetConfigurationAsync();
                
                // Validate basic structure
                if (config.Suppliers == null || !config.Suppliers.Any())
                {
                    result.AddError("No suppliers configured");
                    return result;
                }

                // Validate each supplier
                var supplierNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var supplier in config.Suppliers)
                {
                    ValidateSupplier(supplier, result, supplierNames);
                }

                result.IsValid = !result.Errors.Any();
                result.AddInfo($"Validated {config.Suppliers.Count} supplier configurations");
            }
            catch (Exception ex)
            {
                result.AddError($"Configuration validation failed: {ex.Message}");
            }

            return result;
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
                foreach (var supplier in _configuration!.Suppliers)
                {
                    if (supplier.Detection?.FileNamePatterns?.Any() == true)
                    {
                        foreach (var pattern in supplier.Detection.FileNamePatterns)
                        {
                            // Convert pattern to lowercase for case-insensitive matching
                            var patternLowerCase = pattern.ToLowerInvariant();
                            if (IsFileNameMatch(fileNameLowerCase, patternLowerCase))
                            {
                                _logger?.LogDebug("Matched supplier '{SupplierName}' with pattern '{Pattern}' (as '{PatternLower}') for file '{FileName}'", 
                                    supplier.Name, pattern, patternLowerCase, fileName);
                                
                                // Set parent reference before returning
                                supplier.ParentConfiguration = _configuration;
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
            // Configuration is loaded in constructor, no need to reload from file
            await Task.CompletedTask;
        }

        private async Task SaveConfigurationAsync(SuppliersConfiguration config, string? filePath = null)
        {
            // Since we're loading from IConfiguration, we don't support saving back to file
            // This method is kept for interface compatibility
            await Task.CompletedTask;
            _configuration = config;
        }

        private void ValidateSupplier(SupplierConfiguration supplier, ConfigurationValidationResult result, HashSet<string> supplierNames)
        {
            // Check for required fields
            if (string.IsNullOrWhiteSpace(supplier.Name))
            {
                result.AddError("Supplier name is required");
                return;
            }

            // Check for duplicate names
            if (supplierNames.Contains(supplier.Name))
            {
                result.AddError($"Duplicate supplier name: {supplier.Name}");
            }
            else
            {
                supplierNames.Add(supplier.Name);
            }

            // Validate detection configuration
            if (supplier.Detection == null)
            {
                result.AddError($"Supplier '{supplier.Name}' missing detection configuration");
            }
            else if (!supplier.Detection.FileNamePatterns.Any())
            {
                result.AddWarning($"Supplier '{supplier.Name}' has no detection patterns");
            }
        }
    }

    /// <summary>
    /// Result of configuration validation
    /// /// </summary>
    public class ConfigurationValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public List<string> Info { get; set; } = new();

        public void AddError(string error) => Errors.Add(error);
        public void AddWarning(string warning) => Warnings.Add(warning);
        public void AddInfo(string info) => Info.Add(info);
    }
}