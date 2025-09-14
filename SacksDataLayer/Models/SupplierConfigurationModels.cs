using System.Text.Json.Serialization;

using Microsoft.EntityFrameworkCore.Metadata.Internal;

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
                    result.Add(column.Key);
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
    public class ColumnProperty : ProductPropertyDefinition
    {

        // File Processing Specific Properties (Excel column mapping)

        [JsonPropertyName("format")]
        public string? Format { get; set; } // For dates, numbers, etc.

        [JsonPropertyName("allowedValues")]
        public List<string> AllowedValues { get; set; } = new();

        internal void ResolveFromMarketConfig(ProductPropertyConfiguration effectiveConfig)
        {
            if(effectiveConfig.Properties == null || effectiveConfig.Properties.Count == 0) return;
            if (string.IsNullOrWhiteSpace(Key)) return;
            if (!effectiveConfig.Properties.TryGetValue(Key, out var def)) return;
            // Merge market definition into this column property
            DisplayName = def.DisplayName;
            DataType = def.DataType;
            MaxLength = def.MaxLength;
            Description = def.Description;
            Classification = def.Classification;
            IsRequired = IsRequired || def.IsRequired;
            if (def.Transformations.Count > 0)
            {
                Transformations.InsertRange(0,def.Transformations);
            }
            if (def.ValidationPatterns.Count > 0)
            {
                ValidationPatterns.InsertRange(0, def.ValidationPatterns);
            }
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