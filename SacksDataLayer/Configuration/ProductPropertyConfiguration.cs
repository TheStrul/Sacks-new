using System.Text.Json;
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
            // Merge market definition into this column property
            DisplayName = def.DisplayName;
            DataType = def.DataType;
            MaxLength = def.MaxLength;
            Description = def.Description;
            Classification = def.Classification;
            IsRequired = IsRequired || def.IsRequired;
            if (def.Transformations.Count > 0)
            {
                Transformations.InsertRange(0, def.Transformations);
            }
            if (def.ValidationPatterns.Count > 0)
            {
                ValidationPatterns.InsertRange(0, def.ValidationPatterns);
            }
        }
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

    /// <summary>
    /// Configuration manager for product property definitions
    /// </summary>
    public class ProductPropertyConfigurationManager
    {
        private static readonly JsonSerializerOptions DefaultOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private readonly Dictionary<string, ProductPropertyConfiguration> _configurations = new();

        /// <summary>
        /// Loads configuration from JSON file
        /// </summary>
        public async Task<ProductPropertyConfiguration> LoadConfigurationAsync(string filePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Configuration file not found: {filePath}");

            var json = await File.ReadAllTextAsync(filePath);
            var config = System.Text.Json.JsonSerializer.Deserialize<ProductPropertyConfiguration>(json)
                ?? throw new InvalidOperationException($"Failed to deserialize configuration from {filePath}");

            _configurations[config.ProductType] = config;
            return config;
        }

        /// <summary>
        /// Gets configuration for a specific product type
        /// </summary>
        public ProductPropertyConfiguration? GetConfiguration(string productType)
        {
            return _configurations.TryGetValue(productType, out var config) ? config : null;
        }

        /// <summary>
        /// Saves configuration to JSON file
        /// </summary>
        public async Task SaveConfigurationAsync(ProductPropertyConfiguration configuration, string filePath)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

            var json = System.Text.Json.JsonSerializer.Serialize(configuration, DefaultOptions);
            
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(filePath, json);
        }
    }
}
