using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace SacksDataLayer.Configuration
{
    /// <summary>
    /// Configuration for dynamic product properties and their classifications
    /// </summary>
    public class ProductPropertyConfiguration
    {

        [JsonPropertyName("productType")]
        public string ProductType { get; set; } = string.Empty;

        [JsonPropertyName("properties")]
        public Dictionary<string, ProductPropertyDefinition> Properties { get; set; } = new();

    }


    /// <summary>
    /// Definition of a single product property
    /// </summary>
    public class ProductPropertyDefinition
    {
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;

        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; } = string.Empty;

        [JsonPropertyName("dataType")]
        public PropertyDataType DataType { get; set; } = PropertyDataType.String; // Technical data type for processing (string, int, decimal, bool, etc.)

        [JsonPropertyName("maxLength")]
        public int? MaxLength { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("classification")]
        public PropertyClassificationType Classification { get; set; } = PropertyClassificationType.ProductDynamic;

        [JsonPropertyName("isRequired")]
        public bool IsRequired { get; set; } = false;

        [JsonPropertyName("skipEntireRow")]
        // is check after all validations - if true, the entire row is skipped if this column fails validation
        public bool SkipEntireRow { get; set; } = false;

        // New: default transformations defined at the market (property) level
        [JsonPropertyName("transformations")]
        public List<string> Transformations { get; set; } = new();


        [JsonPropertyName("validationPatterns")]
        public List<string> ValidationPatterns { get; set; } = new();


        [JsonPropertyName("format")]
        public string? Format { get; set; } // For dates, numbers, etc.

        [JsonPropertyName("allowedValues")]
        public List<string> AllowedValues { get; set; } = new();

        internal void ResolveFromMarketConfig(ProductPropertyConfiguration effectiveConfig)
        {
            if (effectiveConfig.Properties == null || effectiveConfig.Properties.Count == 0) return;
            if (string.IsNullOrWhiteSpace(Key)) return;
            if (!effectiveConfig.Properties.TryGetValue(Key, out var def)) return;

            // Merge market definition into this column property, but prefer supplier-provided values.
            // Only set supplier properties when they are missing/default.

            if (string.IsNullOrWhiteSpace(DisplayName) && !string.IsNullOrWhiteSpace(def.DisplayName))
            {
                DisplayName = def.DisplayName;
            }

            if (MaxLength == null && def.MaxLength != null)
            {
                MaxLength = def.MaxLength;
            }

            if (string.IsNullOrWhiteSpace(Description) && !string.IsNullOrWhiteSpace(def.Description))
            {
                Description = def.Description;
            }

            // Classification: do not overwrite an explicit supplier classification.
            // If supplier left the default (ProductDynamic) and market defines something else, apply market classification.
            if (Classification == PropertyClassificationType.ProductDynamic && def.Classification != PropertyClassificationType.ProductDynamic)
            {
                Classification = def.Classification;
            }

            // DataType: only apply market data type when supplier left default (String)
            if (DataType == PropertyDataType.String && def.DataType != PropertyDataType.String)
            {
                DataType = def.DataType;
            }

            // Required/skip flags: treat supplier flag as authoritative; if supplier left default false, inherit market true
            IsRequired = IsRequired || def.IsRequired;
            SkipEntireRow = SkipEntireRow || def.SkipEntireRow;

            // Transformations/validation: if supplier provides none, inherit market defaults; otherwise keep supplier list
            if ((Transformations == null || Transformations.Count == 0) && def.Transformations != null && def.Transformations.Count > 0)
            {
                Transformations = new List<string>(def.Transformations);
            }
            if ((ValidationPatterns == null || ValidationPatterns.Count == 0) && def.ValidationPatterns != null && def.ValidationPatterns.Count > 0)
            {
                ValidationPatterns = new List<string>(def.ValidationPatterns);
            }

            // Formatting and allowed values: only fill when missing
            if (string.IsNullOrWhiteSpace(Format) && !string.IsNullOrWhiteSpace(def.Format))
            {
                Format = def.Format;
            }
            if ((AllowedValues == null || AllowedValues.Count == 0) && def.AllowedValues != null && def.AllowedValues.Count > 0)
            {
                AllowedValues = new List<string>(def.AllowedValues);
            }
        }
    }


    /// <summary>
    /// Represents a property that can extract multiple sub-properties from a single column value
    /// 
    /// Example JSON configuration:
    /// "extendedProperties": {
    ///   "A": {
    ///     "transformProperties": [
    ///       {
    ///         "key": "Brand",
    ///         "transformation": "extractpattern",
    ///         "parameters": "Brand:(?&lt;Brand&gt;[^|]+)"
    ///       },
    ///       {
    ///         "key": "Type",
    ///         "transformation": "extractpattern", 
    ///         "parameters": "Type:(?&lt;Type&gt;[^|]+)"
    ///       }
    ///     ]
    ///   },
    ///   "C": {
    ///     "transformProperties": [
    ///       {
    ///         "key": "Size",
    ///         "transformation": "extractsizeandunits",
    ///         "parameters": "Size,Units"
    ///       }
    ///     ]
    ///   }
    /// }
    /// </summary>
    public class ExtractedProperty
    {
        /// <summary>
        /// Collection of transformation properties to extract from the source column
        /// </summary>
        public List<TransformProps> TransformProperties { get; set; } = new();
    }

    /// <summary>
    /// Defines a transformation to extract a specific property
    /// </summary>
    public class TransformProps
    {
        /// <summary>
        /// The key/name of the property to extract (e.g., "Brand", "Price", "Type")
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// The transformation to apply (e.g., "UpperCase", "ExtractUpperWordsFromStart", "mapValue", "extractpattern")
        /// </summary>
        public string Transformation { get; set; } = string.Empty;

        /// <summary>
        /// Parameters for the transformation, if needed
        /// </summary>
        public string Parameters { get; set; } = string.Empty;

        public PropertyClassificationType Classification = PropertyClassificationType.ProductDynamic;
    }


    /// <summary>
    /// Property data types
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PropertyDataType
    {
        String,
        Integer,
        Decimal,
        Boolean,
        DateTime,
        Array
    }

    /// <summary>
    /// Property classification types - one enum value per property for maximum precision
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PropertyClassificationType
    {
        // === FIXED PRODUCT ENTITY PROPERTIES ===
        /// <summary>
        /// Product name - stored in ProductEntity.Name
        /// </summary>
        ProductName,

        /// <summary>
        /// European Article Number - stored in ProductEntity.EAN
        /// </summary>
        ProductEAN,

        // === FIXED OFFER ENTITY PROPERTIES ===
        /// <summary>
        /// Product price - stored in ProductOfferAnnex.Price (required)
        /// </summary>
        OfferPrice,

        /// <summary>
        /// Currency for the price - stored in ProductOfferAnnex.OfferProperties["Currency"]
        /// </summary>
        OfferCurrency,

        /// <summary>
        /// Available quantity - stored in ProductOfferAnnex.Quantity (required)
        /// </summary>
        OfferQuantity,

        /// <summary>
        /// Supplier's product description - stored in ProductOfferAnnex.Description
        /// </summary>
        OfferDescription,

        /// <summary>
        /// Generic core product property - stored in ProductEntity.DynamicProperties[key]
        /// </summary>
        ProductDynamic,

        /// <summary>
        /// Generic offer property - stored in ProductOfferAnnex.OfferProperties[key]
        /// </summary>
        OfferDynamic,

    }

}
