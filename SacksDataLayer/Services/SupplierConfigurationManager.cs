using System.Text.Json;
using System.Text.Json.Serialization;
using SacksDataLayer.FileProcessing.Configuration;
using Microsoft.Extensions.Logging;

namespace SacksDataLayer.FileProcessing.Services
{
    /// <summary>
    /// Manages loading and updating supplier configurations from JSON files
    /// </summary>
    public class SupplierConfigurationManager
    {
        private readonly string _configurationFilePath;
        private SuppliersConfiguration? _configuration;
        private DateTime _lastLoadTime;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly ILogger<SupplierConfigurationManager>? _logger;

        /// <summary>
        /// Creates a new instance with automatic configuration file discovery
        /// </summary>
        public SupplierConfigurationManager(ILogger<SupplierConfigurationManager>? logger = null)
            : this(FindConfigurationFile(), logger)
        {
        }

        /// <summary>
        /// Creates a new instance with specified configuration file path
        /// </summary>
        public SupplierConfigurationManager(string configurationFilePath, ILogger<SupplierConfigurationManager>? logger = null)
        {
            _configurationFilePath = configurationFilePath ?? throw new ArgumentNullException(nameof(configurationFilePath));
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
                AllowTrailingCommas = true,
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
            };
        }

        /// <summary>
        /// Automatically finds the supplier configuration file in common locations
        /// </summary>
        /// <returns>Path to the configuration file</returns>
        /// <exception cref="FileNotFoundException">Thrown when configuration file is not found</exception>
        public static string FindConfigurationFile()
        {
            var possiblePaths = new[]
            {
                Path.Combine("..", "SacksDataLayer", "Configuration", "supplier-formats.json"),
                Path.Combine("SacksDataLayer", "Configuration", "supplier-formats.json"),
                Path.Combine("Configuration", "supplier-formats.json"),
                "supplier-formats.json"
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            throw new FileNotFoundException("Configuration file 'supplier-formats.json' not found in any of the expected locations.");
        }

        /// <summary>
        /// Gets all supplier configurations
        /// </summary>
        public async Task<SuppliersConfiguration> GetConfigurationAsync()
        {
            await EnsureConfigurationLoadedAsync();
            return _configuration!;
        }

        /// <summary>
        /// Gets configuration for a specific supplier by name
        /// </summary>
        public async Task<SupplierConfiguration?> GetSupplierConfigurationAsync(string supplierName)
        {
            var config = await GetConfigurationAsync();
            return config.Suppliers.FirstOrDefault(s => 
                string.Equals(s.Name, supplierName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets all supplier configurations ordered by name
        /// </summary>
        public async Task<List<SupplierConfiguration>> GetSupplierConfigurationsByPriorityAsync()
        {
            var config = await GetConfigurationAsync();
            return config.Suppliers
                .OrderBy(s => s.Name)
                .ToList();
        }

        /// <summary>
        /// Adds a new supplier configuration
        /// </summary>
        public async Task AddSupplierConfigurationAsync(SupplierConfiguration supplierConfig)
        {
            var config = await GetConfigurationAsync();
            
            // Remove existing configuration with same name
            config.Suppliers.RemoveAll(s => 
                string.Equals(s.Name, supplierConfig.Name, StringComparison.OrdinalIgnoreCase));
            
            // Add new configuration
            config.Suppliers.Add(supplierConfig);
            config.LastUpdated = DateTime.UtcNow;
            
            await SaveConfigurationAsync(config);
        }

        /// <summary>
        /// Updates an existing supplier configuration
        /// </summary>
        public async Task UpdateSupplierConfigurationAsync(SupplierConfiguration supplierConfig)
        {
            await AddSupplierConfigurationAsync(supplierConfig); // Same implementation
        }

        /// <summary>
        /// Removes a supplier configuration
        /// </summary>
        public async Task RemoveSupplierConfigurationAsync(string supplierName)
        {
            var config = await GetConfigurationAsync();
            var removed = config.Suppliers.RemoveAll(s => 
                string.Equals(s.Name, supplierName, StringComparison.OrdinalIgnoreCase));
            
            if (removed > 0)
            {
                config.LastUpdated = DateTime.UtcNow;
                await SaveConfigurationAsync(config);
            }
        }

        /// <summary>
        /// Reloads configuration from file
        /// </summary>
        public async Task ReloadConfigurationAsync()
        {
            _configuration = null;
            await EnsureConfigurationLoadedAsync();
        }

        /// <summary>
        /// Validates the configuration file
        /// </summary>
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

                // Remove priority-based validation since Priority no longer exists
                // var priorities = config.Suppliers
                //     .Where(s => s.Detection.Priority > 1)
                //     .GroupBy(s => s.Detection.Priority)
                //     .Where(g => g.Count() > 1);

                // foreach (var group in priorities)
                // {
                //     var suppliers = string.Join(", ", group.Select(s => s.Name));
                //     result.AddWarning($"Multiple suppliers have priority {group.Key}: {suppliers}");
                // }

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
        /// </summary>
        public async Task<SupplierConfiguration?> DetectSupplierFromFileAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
                
            try
            {
                var fileName = Path.GetFileName(filePath);
                _logger?.LogInformation("Detecting supplier configuration for file: {FileName}", fileName);
                
                var config = await GetConfigurationAsync();

                // Try to match against each supplier's file name patterns
                foreach (var supplier in config.Suppliers)
                {
                    if (supplier.Detection?.FileNamePatterns?.Any() == true)
                    {
                        foreach (var pattern in supplier.Detection.FileNamePatterns)
                        {
                            if (IsFileNameMatch(fileName, pattern))
                            {
                                _logger?.LogInformation("Matched supplier '{SupplierName}' with pattern '{Pattern}' for file '{FileName}'", 
                                    supplier.Name, pattern, fileName);
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
        /// </summary>
        private bool IsFileNameMatch(string fileName, string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern) || string.IsNullOrWhiteSpace(fileName))
                return false;

            // Convert pattern to regex
            var regexPattern = pattern
                .Replace("*", ".*")
                .Replace("?", ".");

            return System.Text.RegularExpressions.Regex.IsMatch(fileName, regexPattern, 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }


        private async Task EnsureConfigurationLoadedAsync()
        {
            if (_configuration == null || ShouldReload())
            {
                await LoadConfigurationAsync();
            }
        }

        private bool ShouldReload()
        {
            try
            {
                var fileInfo = new FileInfo(_configurationFilePath);
                return fileInfo.Exists && fileInfo.LastWriteTime > _lastLoadTime;
            }
            catch
            {
                return false;
            }
        }

        private async Task LoadConfigurationAsync()
        {
            try
            {
                if (!File.Exists(_configurationFilePath))
                {
                    // Create default configuration if file doesn't exist
                    await CreateDefaultConfigurationAsync();
                }

                var json = await File.ReadAllTextAsync(_configurationFilePath);
                _configuration = JsonSerializer.Deserialize<SuppliersConfiguration>(json, _jsonOptions);
                _lastLoadTime = DateTime.UtcNow;

                if (_configuration == null)
                {
                    throw new InvalidOperationException("Failed to deserialize configuration file");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load configuration from '{_configurationFilePath}': {ex.Message}", ex);
            }
        }

        private async Task SaveConfigurationAsync(SuppliersConfiguration config, string? filePath = null)
        {
            var targetPath = filePath ?? _configurationFilePath;
            
            // Ensure directory exists
            var directory = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(config, _jsonOptions);
            await File.WriteAllTextAsync(targetPath, json);
            
            if (filePath == null) // Only update cache if saving to main file
            {
                _configuration = config;
                _lastLoadTime = DateTime.UtcNow;
            }
        }

        private async Task CreateDefaultConfigurationAsync()
        {
            var defaultConfig = new SuppliersConfiguration
            {
                Version = "2.1",
                LastUpdated = DateTime.UtcNow,
                Suppliers = new List<SupplierConfiguration>
                {
                    CreateGenericSupplierConfiguration()
                }
            };

            await SaveConfigurationAsync(defaultConfig);
        }

        private SupplierConfiguration CreateGenericSupplierConfiguration()
        {
            return new SupplierConfiguration
            {
                Name = "Generic",
                Metadata = new SupplierMetadata 
                { 
                    Industry = "Default configuration for unknown suppliers" 
                },
                Detection = new DetectionConfiguration
                {
                    FileNamePatterns = new List<string> { "*" }
                },
                ColumnProperties = new Dictionary<string, ColumnProperty>
                {
                    { "A", new ColumnProperty { TargetProperty = "Name", DisplayName = "Name", DataType = new DataTypeConfiguration { Type = "string", AllowNull = false }, Classification = "coreProduct" } },
                    { "B", new ColumnProperty { TargetProperty = "EAN", DisplayName = "EAN", DataType = new DataTypeConfiguration { Type = "string", AllowNull = true }, Classification = "coreProduct" } },
                    { "C", new ColumnProperty { TargetProperty = "Price", DisplayName = "Price", DataType = new DataTypeConfiguration { Type = "decimal", DefaultValue = 0 }, Classification = "offer" } }
                },
                FileStructure = new FileStructureConfiguration
                {
                    DataStartRowIndex = 2,
                    ExpectedColumnCount = 10,
                },
                Transformation = new TransformationConfiguration
                {
                    SkipEmptyRows = true,
                    TrimWhitespace = true
                }
            };
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

            // Validate column mappings
            if (supplier.ColumnIndexMappings == null || !supplier.ColumnIndexMappings.Any())
            {
                result.AddWarning($"Supplier '{supplier.Name}' has no column index mappings");
            }
        }
    }

    /// <summary>
    /// Result of configuration validation
    /// </summary>
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