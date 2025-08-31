using System.Text.Json.Serialization;
using SacksDataLayer.FileProcessing.Models;

namespace SacksDataLayer.FileProcessing.Configuration
{
    /// <summary>
    /// Root configuration containing all supplier configurations
    /// </summary>
    public class SuppliersConfiguration
    {
        [JsonPropertyName("suppliers")]
        public List<SupplierConfiguration> Suppliers { get; set; } = new();

        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0";

        [JsonPropertyName("lastUpdated")]
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("description")]
        public string Description { get; set; } = "Configuration file for all supplier file formats";
    }

    /// <summary>
    /// Configuration for a specific supplier
    /// </summary>
    public class SupplierConfiguration
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("detection")]
        public DetectionConfiguration Detection { get; set; } = new();

        [JsonPropertyName("columnMappings")]
        public Dictionary<string, string> ColumnMappings { get; set; } = new();

        [JsonPropertyName("columnIndexMappings")]
        public Dictionary<string, string> ColumnIndexMappings { get; set; } = new();

        [JsonPropertyName("propertyClassification")]
        public PropertyClassificationConfiguration PropertyClassification { get; set; } = new();

        [JsonPropertyName("dataTypes")]
        public Dictionary<string, DataTypeConfiguration> DataTypes { get; set; } = new();

        [JsonPropertyName("validation")]
        public ValidationConfiguration Validation { get; set; } = new();

        [JsonPropertyName("transformation")]
        public TransformationConfiguration Transformation { get; set; } = new();

        [JsonPropertyName("metadata")]
        public SupplierMetadata Metadata { get; set; } = new();

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
    /// Configuration for data type parsing and conversion
    /// </summary>
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

        [JsonPropertyName("transformations")]
        public List<string> Transformations { get; set; } = new(); // e.g., "trim", "lowercase", "removeSymbols"
    }

    /// <summary>
    /// Configuration for data validation rules
    /// </summary>
    public class ValidationConfiguration
    {
        [JsonPropertyName("dataStartRowIndex")]
        public int DataStartRowIndex { get; set; } = 1; // 1-based index (Excel row number)

        [JsonPropertyName("expectedColumnCount")]
        public int ExpectedColumnCount { get; set; } = 0;
    }

    /// <summary>
    /// Configuration for data transformations
    /// </summary>
    public class TransformationConfiguration
    {
        [JsonPropertyName("headerRowIndex")]
        public int HeaderRowIndex { get; set; } = 0; // 0-based index

        [JsonPropertyName("dataStartRowIndex")]
        public int DataStartRowIndex { get; set; } = 1; // 0-based index

        [JsonPropertyName("skipEmptyRows")]
        public bool SkipEmptyRows { get; set; } = true;

        [JsonPropertyName("trimWhitespace")]
        public bool TrimWhitespace { get; set; } = true;

        [JsonPropertyName("subtitleRowHandling")]
        public SubtitleRowHandlingConfiguration? SubtitleRowHandling { get; set; }

        [JsonPropertyName("skipRowsWithMergedCells")]
        public bool SkipRowsWithMergedCells { get; set; } = false;

        [JsonPropertyName("useColumnIndexMapping")]
        public bool UseColumnIndexMapping { get; set; } = false;

        [JsonPropertyName("ignoreHeaderRow")]
        public bool IgnoreHeaderRow { get; set; } = false;

        [JsonPropertyName("customTransformations")]
        public Dictionary<string, string> CustomTransformations { get; set; } = new();
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

        [JsonPropertyName("notes")]
        public List<string> Notes { get; set; } = new();

        [JsonPropertyName("lastUpdated")]
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0";
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
    /// Configuration for classifying properties as core product vs offer-specific
    /// </summary>
    public class PropertyClassificationConfiguration
    {
        [JsonPropertyName("coreProductProperties")]
        public List<string> CoreProductProperties { get; set; } = new();

        [JsonPropertyName("offerProperties")]
        public List<string> OfferProperties { get; set; } = new();
    }

    /// <summary>
    /// Configuration for handling subtitle rows in Excel files
    /// </summary>
    public class SubtitleRowHandlingConfiguration
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = false;

        [JsonPropertyName("action")]
        public string Action { get; set; } = "skip"; // "skip" or "parse"

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
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("detectionMethod")]
        public string DetectionMethod { get; set; } = "columnCount"; // "columnCount", "pattern", "hybrid"

        [JsonPropertyName("expectedColumnCount")]
        public int ExpectedColumnCount { get; set; } = 1;

        [JsonPropertyName("columnIndexMappings")]
        public Dictionary<string, string> ColumnIndexMappings { get; set; } = new();

        [JsonPropertyName("applyToSubsequentRows")]
        public bool ApplyToSubsequentRows { get; set; } = true;

        [JsonPropertyName("validationPatterns")]
        public List<string> ValidationPatterns { get; set; } = new();
    }
}