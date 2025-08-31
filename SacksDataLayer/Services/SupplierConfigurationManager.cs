using System.Text.Json;
using System.Text.Json.Serialization;
using SacksDataLayer.FileProcessing.Configuration;

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

        public SupplierConfigurationManager(string configurationFilePath = "Configuration/supplier-formats.json")
        {
            _configurationFilePath = configurationFilePath;
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
        /// Gets all supplier configurations ordered by priority (highest first)
        /// </summary>
        public async Task<List<SupplierConfiguration>> GetSupplierConfigurationsByPriorityAsync()
        {
            var config = await GetConfigurationAsync();
            return config.Suppliers
                .OrderByDescending(s => s.Detection.Priority)
                .ThenBy(s => s.Name)
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

                // Check for duplicate priorities (warning only)
                var priorities = config.Suppliers
                    .Where(s => s.Detection.Priority > 1)
                    .GroupBy(s => s.Detection.Priority)
                    .Where(g => g.Count() > 1);

                foreach (var group in priorities)
                {
                    var suppliers = string.Join(", ", group.Select(s => s.Name));
                    result.AddWarning($"Multiple suppliers have priority {group.Key}: {suppliers}");
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
        /// Creates a sample configuration file
        /// </summary>
        public async Task CreateSampleConfigurationAsync(string filePath)
        {
            var sampleConfig = new SuppliersConfiguration
            {
                Version = "1.0",
                Description = "Sample supplier configurations",
                LastUpdated = DateTime.UtcNow,
                Suppliers = new List<SupplierConfiguration>
                {
                    CreateSampleSupplierConfiguration("SampleSupplier")
                }
            };

            await SaveConfigurationAsync(sampleConfig, filePath);
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
                Version = "1.0",
                Description = "Default supplier configurations",
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
                Description = "Default configuration for unknown suppliers",
                Detection = new DetectionConfiguration
                {
                    FileNamePatterns = new List<string> { "*" },
                    Priority = 1
                },
                ColumnMappings = new Dictionary<string, string>
                {
                    { "Product Name", "Name" },
                    { "Name", "Name" },
                    { "Description", "Description" },
                    { "SKU", "SKU" },
                    { "Price", "Price" },
                    { "Category", "Category" }
                },
                DataTypes = new Dictionary<string, DataTypeConfiguration>
                {
                    { "Price", new DataTypeConfiguration { Type = "decimal", DefaultValue = 0 } },
                    { "StockQuantity", new DataTypeConfiguration { Type = "int", DefaultValue = 0 } }
                },
                Validation = new ValidationConfiguration
                {
                    RequiredFields = new List<string> { "Name" },
                    SkipRowsWithoutName = true
                },
                Transformation = new TransformationConfiguration
                {
                    HeaderRowIndex = 0,
                    DataStartRowIndex = 1,
                    SkipEmptyRows = true,
                    TrimWhitespace = true
                }
            };
        }

        private SupplierConfiguration CreateSampleSupplierConfiguration(string name)
        {
            var config = CreateGenericSupplierConfiguration();
            config.Name = name;
            config.Description = $"Sample configuration for {name}";
            config.Detection.FileNamePatterns = new List<string> { $"*{name}*" };
            config.Detection.Priority = 5;
            return config;
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
            else if (!supplier.Detection.FileNamePatterns.Any() && !supplier.Detection.HeaderKeywords.Any())
            {
                result.AddWarning($"Supplier '{supplier.Name}' has no detection patterns");
            }

            // Validate column mappings
            if (supplier.ColumnMappings == null || !supplier.ColumnMappings.Any())
            {
                result.AddWarning($"Supplier '{supplier.Name}' has no column mappings");
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