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

        [JsonIgnore]
        public PropertyClassificationConfiguration? PropertyClassification 
        { 
            get => GetPropertyClassification();
            set => SetPropertyClassification(value);
        }

        [JsonIgnore]
        public Dictionary<string, DataTypeConfiguration>? DataTypes 
        { 
            get => GetDataTypes();
            set => SetDataTypes(value ?? new Dictionary<string, DataTypeConfiguration>());
        }

        [JsonIgnore]
        public ValidationConfiguration? Validation 
        { 
            get => GetValidation();
            set => SetValidation(value);
        }



        /// <summary>
        /// Gets column mappings from ColumnProperties for legacy compatibility
        /// </summary>
        public Dictionary<string, string> GetColumnMappings()
        {
            return ColumnProperties?.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.TargetProperty ?? kvp.Value.DisplayName ?? kvp.Key
            ) ?? new Dictionary<string, string>();
        }

        /// <summary>
        /// Sets column mappings by updating ColumnProperties
        /// </summary>
        public void SetColumnMappings(Dictionary<string, string> mappings)
        {
            ColumnProperties = mappings.ToDictionary(
                kvp => kvp.Key,
                kvp => new ColumnProperty 
                { 
                    TargetProperty = kvp.Value,
                    DisplayName = kvp.Value,
                    DataType = new DataTypeConfiguration { Type = "string", AllowNull = true },
                    Classification = "coreProduct",
                    Validation = new ColumnValidationConfiguration()
                }
            );
        }

        /// <summary>
        /// Gets legacy property classification for backward compatibility
        /// </summary>
        public PropertyClassificationConfiguration GetPropertyClassification()
        {
            var coreProperties = ColumnProperties?
                .Where(kvp => kvp.Value.Classification == "coreProduct")
                .Select(kvp => kvp.Value.TargetProperty ?? kvp.Value.DisplayName ?? kvp.Key)
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList() ?? new List<string>();

            var offerProperties = ColumnProperties?
                .Where(kvp => kvp.Value.Classification == "offer")
                .Select(kvp => kvp.Value.TargetProperty ?? kvp.Value.DisplayName ?? kvp.Key)
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList() ?? new List<string>();

            return new PropertyClassificationConfiguration
            {
                CoreProductProperties = coreProperties!,
                OfferProperties = offerProperties!
            };
        }

        /// <summary>
        /// Sets property classification by updating ColumnProperties
        /// </summary>
        public void SetPropertyClassification(PropertyClassificationConfiguration? config)
        {
            if (config == null) return;

            foreach (var columnProperty in ColumnProperties.Values)
            {
                var targetProperty = columnProperty.TargetProperty ?? columnProperty.DisplayName;
                if (config.CoreProductProperties.Contains(targetProperty))
                {
                    columnProperty.Classification = "coreProduct";
                }
                else if (config.OfferProperties.Contains(targetProperty))
                {
                    columnProperty.Classification = "offer";
                }
            }
        }

        /// <summary>
        /// Gets legacy data types for backward compatibility
        /// </summary>
        public Dictionary<string, DataTypeConfiguration> GetDataTypes()
        {
            return ColumnProperties?.ToDictionary(
                kvp => kvp.Value.TargetProperty ?? kvp.Value.DisplayName ?? kvp.Key,
                kvp => kvp.Value.DataType
            ) ?? new Dictionary<string, DataTypeConfiguration>();
        }

        /// <summary>
        /// Sets data types by updating ColumnProperties
        /// </summary>
        public void SetDataTypes(Dictionary<string, DataTypeConfiguration> dataTypes)
        {
            ArgumentNullException.ThrowIfNull(dataTypes);
            
            foreach (var kvp in dataTypes)
            {
                var columnProperty = ColumnProperties.Values
                    .FirstOrDefault(cp => (cp.TargetProperty ?? cp.DisplayName) == kvp.Key);
                if (columnProperty != null)
                {
                    columnProperty.DataType = kvp.Value;
                }
            }
        }

        /// <summary>
        /// Gets legacy validation configuration for backward compatibility
        /// </summary>
        public ValidationConfiguration GetValidation()
        {
            var requiredFields = ColumnProperties?
                .Where(kvp => kvp.Value.Validation.IsRequired)
                .Select(kvp => kvp.Value.TargetProperty ?? kvp.Value.DisplayName ?? kvp.Key)
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList() ?? new List<string>();

            var uniqueFields = ColumnProperties?
                .Where(kvp => kvp.Value.Validation.IsUnique)
                .Select(kvp => kvp.Value.TargetProperty ?? kvp.Value.DisplayName ?? kvp.Key)
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList() ?? new List<string>();

            return new ValidationConfiguration
            {
                DataStartRowIndex = FileStructure?.DataStartRowIndex ?? 1,
                ExpectedColumnCount = FileStructure?.ExpectedColumnCount ?? 0,
                RequiredFields = requiredFields!,
                UniqueFields = uniqueFields!
            };
        }

        /// <summary>
        /// Sets validation configuration by updating ColumnProperties and FileStructure
        /// </summary>
        public void SetValidation(ValidationConfiguration? config)
        {
            if (config == null) return;

            // Update file structure
            FileStructure.DataStartRowIndex = config.DataStartRowIndex;
            FileStructure.ExpectedColumnCount = config.ExpectedColumnCount;

            // Update column validations
            foreach (var columnProperty in ColumnProperties.Values)
            {
                var targetProperty = columnProperty.TargetProperty ?? columnProperty.DisplayName;
                columnProperty.Validation.IsRequired = config.RequiredFields.Contains(targetProperty);
                columnProperty.Validation.IsUnique = config.UniqueFields.Contains(targetProperty);
            }
        }
    }

    /// <summary>
    /// Unified column property configuration combining mapping, classification, data type, and validation
    /// </summary>
    public class ColumnProperty
    {
        [JsonPropertyName("targetProperty")]
        public string? TargetProperty { get; set; }

        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("dataType")]
        public DataTypeConfiguration DataType { get; set; } = new();

        [JsonPropertyName("classification")]
        public string Classification { get; set; } = "coreProduct"; // "coreProduct" or "offer"

        [JsonPropertyName("validation")]
        public ColumnValidationConfiguration Validation { get; set; } = new();
    }

    /// <summary>
    /// Validation configuration for individual columns
    /// </summary>
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

        [JsonPropertyName("maxLength")]
        public int? MaxLength { get; set; }

        [JsonPropertyName("transformations")]
        public List<string> Transformations { get; set; } = new(); // e.g., "trim", "lowercase", "removeSymbols"
    }

    /// <summary>
    /// Configuration for data validation rules (legacy support)
    /// </summary>
    public class ValidationConfiguration
    {
        [JsonPropertyName("dataStartRowIndex")]
        public int DataStartRowIndex { get; set; } = 5; // 1-based index (Excel row number)

        [JsonPropertyName("expectedColumnCount")]
        public int ExpectedColumnCount { get; set; } = 15;

        [JsonPropertyName("requiredFields")]
        public List<string> RequiredFields { get; set; } = new();

        [JsonPropertyName("uniqueFields")]
        public List<string> UniqueFields { get; set; } = new();
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