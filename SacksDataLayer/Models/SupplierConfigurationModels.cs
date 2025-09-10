using System.Text.Json.Serialization;

using SacksDataLayer.Configuration;

namespace SacksDataLayer.FileProcessing.Configuration
{
    /// <summary>
    /// Root configuration containing all supplier configurations
    /// </summary>
    public class SuppliersConfiguration
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = "2.1";

        [JsonPropertyName("metadata")]
        public string FullPath { get; set; } = ".";
        
        [JsonPropertyName("suppliers")]
        public List<SupplierConfiguration> Suppliers { get; set; } = new();

        [JsonPropertyName("productPropertyConfiguration")]
        public ProductPropertyConfiguration? ProductPropertyConfiguration { get; set; }

        /// <summary>
        /// Resolves all supplier column properties using the embedded market configuration
        /// </summary>
        public void ResolveAllSupplierProperties()
        {
            if (ProductPropertyConfiguration == null) return;

            foreach (var supplier in Suppliers)
            {
                supplier.ParentConfiguration = this;
                supplier.ResolveColumnProperties(ProductPropertyConfiguration);
            }
            // Run a non-failing validation/reporting pass to surface mapping issues
            var report = GetValidationReport();
            if (report.MissingReferencedKeys.Any())
            {
                // Prefer logging - keep non-breaking for now
                System.Diagnostics.Debug.WriteLine("SuppliersConfiguration validation: missing market keys referenced by suppliers: " + string.Join(", ", report.MissingReferencedKeys));
            }
            if (report.UnusedMarketKeys.Any())
            {
                System.Diagnostics.Debug.WriteLine("SuppliersConfiguration validation: unused market keys: " + string.Join(", ", report.UnusedMarketKeys));
            }
        }

        /// <summary>
        /// Gets a supplier configuration by name with resolved properties
        /// </summary>
        public SupplierConfiguration? GetResolvedSupplierConfiguration(string supplierName)
        {
            var supplier = Suppliers.FirstOrDefault(s => 
                string.Equals(s.Name, supplierName, StringComparison.OrdinalIgnoreCase));
            
            if (supplier != null)
            {
                supplier.ParentConfiguration = this;
                if (ProductPropertyConfiguration != null)
                {
                    supplier.ResolveColumnProperties(ProductPropertyConfiguration);
                }
            }
            
            return supplier;
        }

        /// <summary>
        /// Validation report describing mapping gaps between suppliers and market configuration
        /// </summary>
        public ValidationReport GetValidationReport()
        {
            var report = new ValidationReport();

            var marketKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (ProductPropertyConfiguration?.Properties != null)
            {
                foreach (var k in ProductPropertyConfiguration.Properties.Keys)
                    marketKeys.Add(k);
            }

            var referenced = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var supplier in Suppliers)
            {
                if (supplier.ColumnProperties == null) continue;
                foreach (var kvp in supplier.ColumnProperties)
                {
                    var productKey = kvp.Value?.ProductPropertyKey;
                    if (!string.IsNullOrWhiteSpace(productKey)) referenced.Add(productKey);
                }
            }

            // Missing: referenced by suppliers but not present in market config
            report.MissingReferencedKeys = referenced.Where(k => !marketKeys.Contains(k)).OrderBy(k => k).ToList();

            // Unused: present in market config but not referenced by any supplier
            report.UnusedMarketKeys = marketKeys.Where(k => !referenced.Contains(k)).OrderBy(k => k).ToList();

            return report;
        }

        /// <summary>
        /// Ensures all suppliers have their parent reference set
        /// </summary>
        public void EnsureParentReferences()
        {
            foreach (var supplier in Suppliers)
            {
                supplier.ParentConfiguration = this;
            }
        }
    }

    /// <summary>
    /// Simple DTO returned by GetValidationReport describing missing/unused keys
    /// </summary>
    public class ValidationReport
    {
        public List<string> MissingReferencedKeys { get; set; } = new();
        public List<string> UnusedMarketKeys { get; set; } = new();
    }

    /// <summary>
    /// Configuration for a specific supplier
    /// </summary>
    public class SupplierConfiguration
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    [JsonPropertyName("detection")]
    public DetectionConfiguration? Detection { get; set; }

        public string Currency { get; set; } = "$";

        [JsonPropertyName("columnProperties")]
        public Dictionary<string, ColumnProperty> ColumnProperties { get; set; } = new();

        [JsonPropertyName("fileStructure")]
        public FileStructureConfiguration FileStructure { get; set; } = new();

        [JsonPropertyName("subtitleHandling")]
        public SubtitleRowHandlingConfiguration? SubtitleHandling { get; set; }

        /// <summary>
        /// Reference to parent configuration (not serialized)
        /// </summary>
        [JsonIgnore]
        public SuppliersConfiguration? ParentConfiguration { get; set; }

        /// <summary>
        /// Gets the effective market configuration (from parent if available)
        /// </summary>
        [JsonIgnore]
        public ProductPropertyConfiguration? EffectiveMarketConfiguration => 
            ParentConfiguration?.ProductPropertyConfiguration;


        /// <summary>
        /// Gets core product properties from column-level classification settings
        /// </summary>
        public List<string> GetCoreProductProperties(ProductPropertyConfiguration? marketConfig = null)
        {
            var result = new List<string>();
            
            if (ColumnProperties == null) return result;

            // Use provided config, or the effective market configuration
            var effectiveConfig = marketConfig ?? EffectiveMarketConfiguration;

            foreach (var kvp in ColumnProperties)
            {
                var column = kvp.Value;
                
                // Resolve from market config if available
                if (effectiveConfig != null)
                {
                    column.ResolveFromMarketConfig(effectiveConfig);
                }
                
                if (column.Classification == PropertyClassificationType.ProductName ||
                   column.Classification == PropertyClassificationType.ProductEAN ||
                   column.Classification == PropertyClassificationType.ProductDynamic)
                {
                    result.Add(column.ProductPropertyKey);
                }
            }
            
            return result.Where(s => !string.IsNullOrEmpty(s)).Distinct().ToList();
        }

        /// <summary>
        /// Gets offer properties from column-level classification settings
        /// </summary>
        public List<string> GetOfferProperties(ProductPropertyConfiguration? marketConfig = null)
        {
            var result = new List<string>();
            
            if (ColumnProperties == null) return result;

            // Use provided config, or the effective market configuration
            var effectiveConfig = marketConfig ?? EffectiveMarketConfiguration;

            foreach (var kvp in ColumnProperties)
            {
                var column = kvp.Value;
                
                // Resolve from market config if available
                if (effectiveConfig != null)
                {
                    column.ResolveFromMarketConfig(effectiveConfig);
                }
                
                if (column.Classification == PropertyClassificationType.OfferPrice ||
                   column.Classification == PropertyClassificationType.OfferQuantity ||
                   column.Classification == PropertyClassificationType.OfferDescription ||
                   column.Classification == PropertyClassificationType.OfferDynamic)
                {
                    result.Add(column.ProductPropertyKey);
                }
            }
            
            return result.Where(s => !string.IsNullOrEmpty(s)).Distinct().ToList();
        }

        /// <summary>
        /// Gets required fields from column-level validation settings
        /// </summary>
        public List<string> GetRequiredFields(ProductPropertyConfiguration? marketConfig = null)
        {
            var result = new List<string>();
            
            if (ColumnProperties == null) return result;

            // Use provided config, or the effective market configuration
            var effectiveConfig = marketConfig ?? EffectiveMarketConfiguration;

            foreach (var kvp in ColumnProperties)
            {
                var column = kvp.Value;
                
                // Resolve from market config if available
                if (effectiveConfig != null)
                {
                    column.ResolveFromMarketConfig(effectiveConfig);
                }
                
                if (column.IsRequired == true)
                {
                    result.Add(column.ProductPropertyKey);
                }
            }
            
            return result.Where(s => !string.IsNullOrEmpty(s)).Distinct().ToList();
        }

        /// <summary>
        /// Resolves all column properties using market configuration
        /// </summary>
        public void ResolveColumnProperties(ProductPropertyConfiguration? marketConfig = null)
        {
            if (ColumnProperties == null) return;

            // Use provided config, or the effective market configuration
            var effectiveConfig = marketConfig ?? EffectiveMarketConfiguration;
            if (effectiveConfig == null) return;

            foreach (var column in ColumnProperties.Values)
            {
                column.ResolveFromMarketConfig(effectiveConfig);
            }
        }

        /// <summary>
        /// Resolves all column properties using the effective market configuration
        /// </summary>
        public void ResolveColumnProperties()
        {
            ResolveColumnProperties(EffectiveMarketConfiguration);
        }
    }

    /// <summary>
    /// Unified column property configuration - references market property definition
    /// </summary>
    public class ColumnProperty
    {
        /// <summary>
        /// Reference to the property key in the market configuration (e.g., "EAN", "Brand", "Category")
        /// This establishes the relationship between Excel column and market property
        /// </summary>
        [JsonPropertyName("productPropertyKey")]
        public string ProductPropertyKey { get; set; } = string.Empty;

        /// <summary>
        /// Classification that determines how this property is processed and stored
        /// Can be explicitly set in configuration or resolved from market configuration
        /// </summary>
        [JsonPropertyName("classification")]
        public PropertyClassificationType Classification { get; set; } = PropertyClassificationType.ProductDynamic;

        // File Processing Specific Properties (Excel column mapping)
        [JsonPropertyName("dataType")]
        public string DataType { get; set; } = "string"; // Technical data type for processing (string, int, decimal, bool, etc.)

        [JsonPropertyName("format")]
        public string? Format { get; set; } // For dates, numbers, etc.

        [JsonPropertyName("maxLength")]
        public int? MaxLength { get; set; }

        [JsonPropertyName("transformations")]
        public List<string> Transformations { get; set; } = new(); // e.g., "trim", "lowercase", "removeSymbols"

        [JsonPropertyName("validationPatterns")]
        public List<string> ValidationPatterns { get; set; } = new();

        [JsonPropertyName("allowedValues")]
        public List<string> AllowedValues { get; set; } = new();

        [JsonPropertyName("skipEntireRow")]
        public bool SkipEntireRow { get; set; } = false;

        // Computed Properties (derived from market configuration)
        [JsonIgnore]
        public string DisplayName { get; set; } = string.Empty; // Will be populated from market config

        [JsonIgnore]
        public bool IsRequired { get; set; } = false; // Will be populated from market config

        /// <summary>
        /// Resolves this column property using the market configuration
        /// If Classification is default/unset, it will be resolved from market config
        /// </summary>
        public void ResolveFromMarketConfig(ProductPropertyConfiguration marketConfig)
        {
            ArgumentNullException.ThrowIfNull(marketConfig);
            
            if (marketConfig.Properties.TryGetValue(ProductPropertyKey, out var marketProperty))
            {
                DisplayName = marketProperty.DisplayName;
                IsRequired = marketProperty.IsRequired;
                
                // Only override classification if it's still the default value
                if (Classification == PropertyClassificationType.ProductDynamic && 
                    ProductPropertyKey != "ProductDynamic") // Avoid overriding explicit ProductDynamic
                {
                    Classification = marketProperty.Classification;
                }
            }
            else
            {
                // Fallback: property not found in market config
                DisplayName = ProductPropertyKey;
                IsRequired = false;
                // Keep existing Classification (don't override)
            }
        }

        /// <summary>
        /// Checks if this column maps to a market property
        /// </summary>
        public bool HasValidMarketMapping(ProductPropertyConfiguration marketConfig)
        {
            if (marketConfig == null) return false;
            
            return !string.IsNullOrEmpty(ProductPropertyKey) && 
                   marketConfig.Properties.ContainsKey(ProductPropertyKey);
        }
    }

    /// <summary>
    /// File structure configuration
    /// </summary>
    public class FileStructureConfiguration
    {
        [JsonPropertyName("dataStartRowIndex")]
        public int DataStartRowIndex { get; set; } = 5; // 1-based index (Excel row number)

        [JsonPropertyName("expectedColumnCount")]
        public int ExpectedColumnCount { get; set; } = 15;

        [JsonPropertyName("headerRowIndex")]
        public int HeaderRowIndex { get; set; } = 4; // 1-based index
    }

    /// <summary>
    /// Configuration for handling subtitle rows in Excel files
    /// </summary>
    public class SubtitleRowHandlingConfiguration
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonPropertyName("action")]
        public string Action { get; set; } = "parse"; // "skip" or "parse"

        [JsonPropertyName("detectionRules")]
        public List<SubtitleDetectionRule> DetectionRules { get; set; } = new();

        [JsonPropertyName("fallbackAction")]
        public string FallbackAction { get; set; } = "skip"; // "skip" or "ignore"
    }

    /// <summary>
    /// Rule for detecting and parsing specific types of subtitle rows
    /// </summary>
    public class SubtitleDetectionRule
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("detectionMethod")]
        public string DetectionMethod { get; set; } = "columnCount"; // "columnCount", "pattern", "hybrid"

        [JsonPropertyName("expectedColumnCount")]
        public int ExpectedColumnCount { get; set; } = 1;

        [JsonPropertyName("applyToSubsequentRows")]
        public bool ApplyToSubsequentRows { get; set; } = true;

        [JsonPropertyName("validationPatterns")]
        public List<string> ValidationPatterns { get; set; } = new();
    }

    /// <summary>
    /// Supplier file detection configuration
    /// </summary>
    public class DetectionConfiguration
    {
        [JsonPropertyName("fileNamePatterns")]
        public List<string> FileNamePatterns { get; set; } = new();

        // Future fields like priority, content-based rules etc. can be added here
    }
}