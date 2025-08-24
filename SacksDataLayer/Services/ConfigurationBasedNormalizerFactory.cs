using SacksDataLayer.FileProcessing.Interfaces;
using SacksDataLayer.FileProcessing.Normalizers;
using SacksDataLayer.FileProcessing.Configuration;

namespace SacksDataLayer.FileProcessing.Services
{
    /// <summary>
    /// Factory for creating normalizers from JSON configuration
    /// </summary>
    public class ConfigurationBasedNormalizerFactory
    {
        private readonly SupplierConfigurationManager _configurationManager;
        private readonly List<ISupplierProductNormalizer> _cachedNormalizers;
        private DateTime _lastCacheUpdate;

        public ConfigurationBasedNormalizerFactory(SupplierConfigurationManager configurationManager)
        {
            _configurationManager = configurationManager ?? throw new ArgumentNullException(nameof(configurationManager));
            _cachedNormalizers = new List<ISupplierProductNormalizer>();
            _lastCacheUpdate = DateTime.MinValue;
        }

        /// <summary>
        /// Gets all normalizers from configuration, ordered by priority
        /// </summary>
        public async Task<List<ISupplierProductNormalizer>> GetAllNormalizersAsync()
        {
            await RefreshCacheIfNeededAsync();
            return new List<ISupplierProductNormalizer>(_cachedNormalizers);
        }

        /// <summary>
        /// Gets a specific normalizer by supplier name
        /// </summary>
        public async Task<ISupplierProductNormalizer?> GetNormalizerAsync(string supplierName)
        {
            var normalizers = await GetAllNormalizersAsync();
            return normalizers.FirstOrDefault(n => 
                string.Equals(n.SupplierName, supplierName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Creates a new normalizer from a supplier configuration
        /// </summary>
        public ISupplierProductNormalizer CreateNormalizer(SupplierConfiguration configuration)
        {
            return new ConfigurationBasedNormalizer(configuration);
        }

        /// <summary>
        /// Adds a new supplier configuration and updates the cache
        /// </summary>
        public async Task AddSupplierConfigurationAsync(SupplierConfiguration configuration)
        {
            await _configurationManager.AddSupplierConfigurationAsync(configuration);
            await RefreshCacheAsync();
        }

        /// <summary>
        /// Updates an existing supplier configuration and refreshes the cache
        /// </summary>
        public async Task UpdateSupplierConfigurationAsync(SupplierConfiguration configuration)
        {
            await _configurationManager.UpdateSupplierConfigurationAsync(configuration);
            await RefreshCacheAsync();
        }

        /// <summary>
        /// Removes a supplier configuration and updates the cache
        /// </summary>
        public async Task RemoveSupplierConfigurationAsync(string supplierName)
        {
            await _configurationManager.RemoveSupplierConfigurationAsync(supplierName);
            await RefreshCacheAsync();
        }

        /// <summary>
        /// Forces a refresh of the normalizer cache
        /// </summary>
        public async Task RefreshCacheAsync()
        {
            _cachedNormalizers.Clear();
            
            var configurations = await _configurationManager.GetSupplierConfigurationsByPriorityAsync();
            
            foreach (var config in configurations)
            {
                try
                {
                    var normalizer = CreateNormalizer(config);
                    _cachedNormalizers.Add(normalizer);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error creating normalizer for supplier '{config.Name}': {ex.Message}");
                }
            }

            _lastCacheUpdate = DateTime.UtcNow;
        }

        /// <summary>
        /// Validates all supplier configurations
        /// </summary>
        public async Task<ConfigurationValidationResult> ValidateConfigurationsAsync()
        {
            return await _configurationManager.ValidateConfigurationAsync();
        }

        /// <summary>
        /// Gets configuration statistics
        /// </summary>
        public async Task<ConfigurationStatistics> GetStatisticsAsync()
        {
            var config = await _configurationManager.GetConfigurationAsync();
            var stats = new ConfigurationStatistics
            {
                TotalSuppliers = config.Suppliers.Count,
                ConfigurationVersion = config.Version,
                LastUpdated = config.LastUpdated,
                LastCacheUpdate = _lastCacheUpdate
            };

            // Group by priority
            stats.SuppliersByPriority = config.Suppliers
                .GroupBy(s => s.Detection.Priority)
                .ToDictionary(g => g.Key, g => g.Select(s => s.Name).ToList());

            // Group by industry
            stats.SuppliersByIndustry = config.Suppliers
                .Where(s => !string.IsNullOrEmpty(s.Metadata.Industry))
                .GroupBy(s => s.Metadata.Industry!)
                .ToDictionary(g => g.Key, g => g.Select(s => s.Name).ToList());

            return stats;
        }

        /// <summary>
        /// Creates a default configuration for a new supplier based on file analysis
        /// </summary>
        public SupplierConfiguration CreateDefaultConfiguration(string supplierName, List<string> columnNames)
        {
            var config = new SupplierConfiguration
            {
                Name = supplierName,
                Description = $"Auto-generated configuration for {supplierName}",
                Detection = new DetectionConfiguration
                {
                    FileNamePatterns = new List<string> { $"*{supplierName}*" },
                    HeaderKeywords = new List<string> { supplierName },
                    RequiredColumns = new List<string>(),
                    Priority = 5
                },
                ColumnMappings = GenerateColumnMappings(columnNames),
                DataTypes = GenerateDataTypes(columnNames),
                Validation = new ValidationConfiguration
                {
                    RequiredFields = new List<string> { "Name" },
                    SkipRowsWithoutName = true,
                    MaxErrorsPerFile = 100
                },
                Transformation = new TransformationConfiguration
                {
                    HeaderRowIndex = 0,
                    DataStartRowIndex = 1,
                    SkipEmptyRows = true,
                    TrimWhitespace = true
                },
                Metadata = new SupplierMetadata
                {
                    Industry = "Unknown",
                    Region = "Unknown",
                    FileFrequency = "Unknown",
                    ExpectedFileSize = "Unknown",
                    Notes = new List<string> { "Auto-generated configuration", "Please review and update as needed" },
                    LastUpdated = DateTime.UtcNow,
                    Version = "1.0"
                }
            };

            return config;
        }

        private async Task RefreshCacheIfNeededAsync()
        {
            var config = await _configurationManager.GetConfigurationAsync();
            
            // Refresh cache if configuration has been updated since last cache update
            if (config.LastUpdated > _lastCacheUpdate || !_cachedNormalizers.Any())
            {
                await RefreshCacheAsync();
            }
        }

        private Dictionary<string, string> GenerateColumnMappings(List<string> columnNames)
        {
            var mappings = new Dictionary<string, string>();
            
            // Common mapping patterns
            var patterns = new Dictionary<string, string[]>
            {
                { "Name", new[] { "name", "product name", "title", "item name", "product title", "product" } },
                { "Description", new[] { "description", "desc", "details", "summary", "product description" } },
                { "SKU", new[] { "sku", "code", "product code", "item code", "id", "product id" } },
                { "Price", new[] { "price", "cost", "unit price", "list price", "retail price", "msrp" } },
                { "Category", new[] { "category", "type", "group", "classification", "product category" } },
                { "StockQuantity", new[] { "stock", "quantity", "inventory", "available", "on hand", "qty" } },
                { "IsActive", new[] { "active", "status", "enabled", "published", "visible" } }
            };

            foreach (var column in columnNames)
            {
                var columnLower = column.ToLowerInvariant();
                
                foreach (var pattern in patterns)
                {
                    if (pattern.Value.Any(p => columnLower.Contains(p)))
                    {
                        mappings[column] = pattern.Key;
                        break;
                    }
                }
            }

            return mappings;
        }

        private Dictionary<string, DataTypeConfiguration> GenerateDataTypes(List<string> columnNames)
        {
            var dataTypes = new Dictionary<string, DataTypeConfiguration>();

            foreach (var column in columnNames)
            {
                var columnLower = column.ToLowerInvariant();
                
                DataTypeConfiguration? dataType = null;

                if (columnLower.Contains("price") || columnLower.Contains("cost"))
                {
                    dataType = new DataTypeConfiguration
                    {
                        Type = "decimal",
                        DefaultValue = 0,
                        AllowNull = true,
                        Transformations = new List<string> { "removeSymbols", "trim" }
                    };
                }
                else if (columnLower.Contains("quantity") || columnLower.Contains("stock") || columnLower.Contains("count"))
                {
                    dataType = new DataTypeConfiguration
                    {
                        Type = "int",
                        DefaultValue = 0,
                        AllowNull = true,
                        Transformations = new List<string> { "trim" }
                    };
                }
                else if (columnLower.Contains("active") || columnLower.Contains("enabled") || columnLower.Contains("status"))
                {
                    dataType = new DataTypeConfiguration
                    {
                        Type = "bool",
                        DefaultValue = true,
                        AllowNull = false,
                        Transformations = new List<string> { "trim", "lowercase" }
                    };
                }
                else if (columnLower.Contains("date") || columnLower.Contains("time"))
                {
                    dataType = new DataTypeConfiguration
                    {
                        Type = "datetime",
                        AllowNull = true,
                        Transformations = new List<string> { "trim" }
                    };
                }

                if (dataType != null)
                {
                    dataTypes[column] = dataType;
                }
            }

            return dataTypes;
        }
    }

    /// <summary>
    /// Statistics about the configuration
    /// </summary>
    public class ConfigurationStatistics
    {
        public int TotalSuppliers { get; set; }
        public string ConfigurationVersion { get; set; } = "";
        public DateTime LastUpdated { get; set; }
        public DateTime LastCacheUpdate { get; set; }
        public Dictionary<int, List<string>> SuppliersByPriority { get; set; } = new();
        public Dictionary<string, List<string>> SuppliersByIndustry { get; set; } = new();
    }
}