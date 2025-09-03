using System.Text.Json.Serialization;
using SacksDataLayer.FileProcessing.Models;

namespace SacksDataLayer.FileProcessing.Configuration
{
    /// <summary>
    /// Root configuration containing all supplier configurations
    /// </summary>
    public class SuppliersConfiguration
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = "2.1";

        [JsonPropertyName("suppliers")]
        public List<SupplierConfiguration> Suppliers { get; set; } = new();
    }

    /// <summary>
    /// Configuration for a specific supplier
    /// </summary>
    public class SupplierConfiguration
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("detection")]
        public DetectionConfiguration Detection { get; set; } = new();

        [JsonPropertyName("columnProperties")]
        public Dictionary<string, ColumnProperty> ColumnProperties { get; set; } = new();

        [JsonPropertyName("fileStructure")]
        public FileStructureConfiguration FileStructure { get; set; } = new();


        /// <summary>
        /// Gets core product properties from column-level classification settings
        /// </summary>
        public List<string> GetCoreProductProperties()
        {
            return ColumnProperties?
                .Where(kvp => kvp.Value.Classification == "coreProduct")
                .Select(kvp => kvp.Value.TargetProperty ?? kvp.Value.DisplayName ?? kvp.Key)
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList() ?? new List<string>();
        }

        /// <summary>
        /// Gets offer properties from column-level classification settings
        /// </summary>
        public List<string> GetOfferProperties()
        {
            return ColumnProperties?
                .Where(kvp => kvp.Value.Classification == "offer")
                .Select(kvp => kvp.Value.TargetProperty ?? kvp.Value.DisplayName ?? kvp.Key)
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList() ?? new List<string>();
        }


        /// <summary>
        /// Gets required fields from column-level validation settings
        /// </summary>
        public List<string> GetRequiredFields()
        {
            return ColumnProperties?
                .Where(kvp => kvp.Value.IsRequired)
                .Select(kvp => kvp.Value.TargetProperty ?? kvp.Value.DisplayName ?? kvp.Key)
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList() ?? new List<string>();
        }

    }

    /// <summary>
    /// Unified column property configuration - flattened structure
    /// </summary>
    public class ColumnProperty
    {
        [JsonPropertyName("targetProperty")]
        public string? TargetProperty { get; set; }

        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; } = string.Empty;

        [JsonPropertyName("classification")]
        public string Classification { get; set; } = "coreProduct"; // "coreProduct" or "offer"

        // Data Type Properties (flattened from DataTypeConfiguration)
        [JsonPropertyName("dataType")]
        public string DataType { get; set; } = "string"; // string, decimal, int, bool, datetime

        [JsonPropertyName("format")]
        public string? Format { get; set; } // For dates, numbers, etc.

        [JsonPropertyName("defaultValue")]
        public object? DefaultValue { get; set; }

        [JsonPropertyName("allowNull")]
        public bool AllowNull { get; set; } = true;

        [JsonPropertyName("maxLength")]
        public int? MaxLength { get; set; }

        [JsonPropertyName("transformations")]
        public List<string> Transformations { get; set; } = new(); // e.g., "trim", "lowercase", "removeSymbols"

        // Validation Properties (flattened from ColumnValidationConfiguration)
        [JsonPropertyName("isRequired")]
        public bool IsRequired { get; set; } = false;

        [JsonPropertyName("isUnique")]
        public bool IsUnique { get; set; } = false;

        [JsonPropertyName("validationPatterns")]
        public List<string> ValidationPatterns { get; set; } = new();

        [JsonPropertyName("allowedValues")]
        public List<string> AllowedValues { get; set; } = new();

        [JsonPropertyName("skipEntireRow")]
        public bool SkipEntireRow { get; set; } = false;
    }

    /// <summary>
    /// Validation configuration for individual columns - DEPRECATED: Properties moved to ColumnProperty
    /// </summary>
    [Obsolete("Use flattened properties in ColumnProperty instead")]
    public class ColumnValidationConfiguration
    {
        [JsonPropertyName("isRequired")]
        public bool IsRequired { get; set; } = false;

        [JsonPropertyName("isUnique")]
        public bool IsUnique { get; set; } = false;

        [JsonPropertyName("validationPatterns")]
        public List<string> ValidationPatterns { get; set; } = new();

        [JsonPropertyName("allowedValues")]
        public List<string> AllowedValues { get; set; } = new();

        [JsonPropertyName("skipEntireRow")]
        public bool SkipEntireRow { get; set; } = false;
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
    /// Configuration for detecting if a file belongs to this supplier
    /// </summary>
    public class DetectionConfiguration
    {
        [JsonPropertyName("fileNamePatterns")]
        public List<string> FileNamePatterns { get; set; } = new();
    }

    /// <summary>
    /// Configuration for data type parsing and conversion - DEPRECATED: Properties moved to ColumnProperty
    /// </summary>
    [Obsolete("Use flattened properties in ColumnProperty instead")]
    public class DataTypeConfiguration
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "string"; // string, decimal, int, bool, datetime

        [JsonPropertyName("format")]
        public string? Format { get; set; } // For dates, numbers, etc.

        [JsonPropertyName("defaultValue")]
        public object? DefaultValue { get; set; }

        [JsonPropertyName("allowNull")]
        public bool AllowNull { get; set; } = true;

        [JsonPropertyName("maxLength")]
        public int? MaxLength { get; set; }

        [JsonPropertyName("transformations")]
        public List<string> Transformations { get; set; } = new(); // e.g., "trim", "lowercase", "removeSymbols"
    }

    /// <summary>
    /// Metadata about the supplier
    /// </summary>
    public class SupplierMetadata
    {
        [JsonPropertyName("industry")]
        public string? Industry { get; set; }

        [JsonPropertyName("region")]
        public string? Region { get; set; }

        [JsonPropertyName("contact")]
        public ContactInfo? Contact { get; set; }

        [JsonPropertyName("fileFrequency")]
        public string? FileFrequency { get; set; } // daily, weekly, monthly

        [JsonPropertyName("expectedFileSize")]
        public string? ExpectedFileSize { get; set; }

        [JsonPropertyName("currency")]
        public string? Currency { get; set; }

        [JsonPropertyName("timezone")]
        public string? Timezone { get; set; }

        [JsonPropertyName("lastUpdated")]
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("version")]
        public string Version { get; set; } = "2.1";

        // Legacy properties for backward compatibility
        [JsonIgnore]
        public List<string> Notes { get; set; } = new();
    }

    /// <summary>
    /// Contact information for the supplier
    /// </summary>
    public class ContactInfo
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("phone")]
        public string? Phone { get; set; }

        [JsonPropertyName("company")]
        public string? Company { get; set; }
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

    // Legacy enum for backward compatibility
    public enum PropertyClassification
    {
        CoreProduct,
        Offer
    }
}