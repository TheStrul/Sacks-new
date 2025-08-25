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

        [JsonPropertyName("processingModes")]
        public ProcessingModeConfiguration ProcessingModes { get; set; } = new();
    }

    /// <summary>
    /// Configuration for detecting if a file belongs to this supplier
    /// </summary>
    public class DetectionConfiguration
    {
        [JsonPropertyName("fileNamePatterns")]
        public List<string> FileNamePatterns { get; set; } = new();

        [JsonPropertyName("headerKeywords")]
        public List<string> HeaderKeywords { get; set; } = new();

        [JsonPropertyName("requiredColumns")]
        public List<string> RequiredColumns { get; set; } = new();

        [JsonPropertyName("excludePatterns")]
        public List<string> ExcludePatterns { get; set; } = new();

        [JsonPropertyName("priority")]
        public int Priority { get; set; } = 0;
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
        [JsonPropertyName("requiredFields")]
        public List<string> RequiredFields { get; set; } = new();

        [JsonPropertyName("skipRowsWithoutName")]
        public bool SkipRowsWithoutName { get; set; } = true;

        [JsonPropertyName("maxErrorsPerFile")]
        public int MaxErrorsPerFile { get; set; } = 100;

        [JsonPropertyName("fieldValidations")]
        public Dictionary<string, FieldValidation> FieldValidations { get; set; } = new();
    }

    /// <summary>
    /// Field-specific validation rules
    /// </summary>
    public class FieldValidation
    {
        [JsonPropertyName("minLength")]
        public int? MinLength { get; set; }

        [JsonPropertyName("maxLength")]
        public int? MaxLength { get; set; }

        [JsonPropertyName("pattern")]
        public string? Pattern { get; set; } // Regex pattern

        [JsonPropertyName("allowedValues")]
        public List<string>? AllowedValues { get; set; }

        [JsonPropertyName("numericRange")]
        public NumericRange? NumericRange { get; set; }
    }

    /// <summary>
    /// Numeric range validation
    /// </summary>
    public class NumericRange
    {
        [JsonPropertyName("min")]
        public decimal? Min { get; set; }

        [JsonPropertyName("max")]
        public decimal? Max { get; set; }
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
    /// Configuration for different processing modes
    /// </summary>
    public class ProcessingModeConfiguration
    {
        [JsonPropertyName("defaultMode")]
        public ProcessingMode DefaultMode { get; set; } = ProcessingMode.UnifiedProductCatalog;

        [JsonPropertyName("supportedModes")]
        public List<ProcessingMode> SupportedModes { get; set; } = new() 
        { 
            ProcessingMode.UnifiedProductCatalog, 
            ProcessingMode.SupplierCommercialData 
        };

        [JsonPropertyName("catalogMode")]
        public ModeSpecificConfiguration CatalogMode { get; set; } = new();

        [JsonPropertyName("commercialMode")]
        public ModeSpecificConfiguration CommercialMode { get; set; } = new();
    }

    /// <summary>
    /// Mode-specific configuration settings
    /// </summary>
    public class ModeSpecificConfiguration
    {
        [JsonPropertyName("priorityColumns")]
        public List<string> PriorityColumns { get; set; } = new();

        [JsonPropertyName("ignoredColumns")]
        public List<string> IgnoredColumns { get; set; } = new();

        [JsonPropertyName("requiredColumns")]
        public List<string> RequiredColumns { get; set; } = new();

        [JsonPropertyName("validationRules")]
        public Dictionary<string, object> ValidationRules { get; set; } = new();

        [JsonPropertyName("transformationRules")]
        public Dictionary<string, object> TransformationRules { get; set; } = new();
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
}