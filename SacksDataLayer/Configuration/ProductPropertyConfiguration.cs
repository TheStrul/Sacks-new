using System.Text.Json;
using System.Text.Json.Serialization;

namespace SacksDataLayer.Configuration
{
    /// <summary>
    /// Configuration for dynamic product properties and their classifications
    /// </summary>
    public class ProductPropertyConfiguration
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0";

        [JsonPropertyName("productType")]
        public string ProductType { get; set; } = string.Empty;

        [JsonPropertyName("properties")]
        public Dictionary<string, ProductPropertyDefinition> Properties { get; set; } = new();

        /// <summary>
        /// Gets all properties classified as core product properties
        /// </summary>
        public List<ProductPropertyDefinition> GetCoreProductProperties()
        {
            return Properties.Values
                .Where(p => p.Classification == PropertyClassificationType.ProductName ||
                           p.Classification == PropertyClassificationType.ProductEAN ||
                           p.Classification == PropertyClassificationType.ProductDynamic)
                .OrderBy(p => p.DisplayOrder)
                .ToList();
        }

        /// <summary>
        /// Gets all properties classified as offer properties
        /// </summary>
        public List<ProductPropertyDefinition> GetOfferProperties()
        {
            return Properties.Values
                .Where(p => p.Classification == PropertyClassificationType.OfferPrice ||
                           p.Classification == PropertyClassificationType.OfferQuantity ||
                           p.Classification == PropertyClassificationType.OfferDescription ||
                           p.Classification == PropertyClassificationType.OfferDynamic)
                .OrderBy(p => p.DisplayOrder)
                .ToList();
        }

        /// <summary>
        /// Gets properties stored in ProductEntity fixed fields
        /// </summary>
        public List<ProductPropertyDefinition> GetProductEntityProperties()
        {
            return Properties.Values
                .Where(p => p.Classification == PropertyClassificationType.ProductName ||
                           p.Classification == PropertyClassificationType.ProductEAN)
                .OrderBy(p => p.DisplayOrder)
                .ToList();
        }

        /// <summary>
        /// Gets properties stored in OfferProductAnnex fixed fields
        /// </summary>
        public List<ProductPropertyDefinition> GetOfferEntityProperties()
        {
            return Properties.Values
                .Where(p => p.Classification == PropertyClassificationType.OfferPrice ||
                           p.Classification == PropertyClassificationType.OfferQuantity ||
                           p.Classification == PropertyClassificationType.OfferDescription)
                .OrderBy(p => p.DisplayOrder)
                .ToList();
        }

        /// <summary>
        /// Gets properties stored in ProductEntity.DynamicProperties
        /// </summary>
        public List<ProductPropertyDefinition> GetDynamicCoreProductProperties()
        {
            return Properties.Values
                .Where(p => p.Classification == PropertyClassificationType.ProductDynamic)
                .OrderBy(p => p.DisplayOrder)
                .ToList();
        }

        /// <summary>
        /// Gets properties stored in OfferProductAnnex.OfferProperties
        /// </summary>
        public List<ProductPropertyDefinition> GetDynamicOfferProperties()
        {
            return Properties.Values
                .Where(p => p.Classification == PropertyClassificationType.OfferDynamic)
                .OrderBy(p => p.DisplayOrder)
                .ToList();
        }
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

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("classification")]
        public PropertyClassificationType Classification { get; set; } = PropertyClassificationType.ProductDynamic;

        [JsonPropertyName("isFilterable")]
        public bool IsFilterable { get; set; } = true;

        [JsonPropertyName("isSortable")]
        public bool IsSortable { get; set; } = true;

        [JsonPropertyName("isRequired")]
        public bool IsRequired { get; set; } = false;

        [JsonPropertyName("isUnique")]
        public bool IsUnique { get; set; } = false;

        [JsonPropertyName("displayOrder")]
        public int DisplayOrder { get; set; } = 0;

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
        /// Product price - stored in OfferProductAnnex.Price (required)
        /// </summary>
        OfferPrice,

        /// <summary>
        /// Available quantity - stored in OfferProductAnnex.Quantity (required)
        /// </summary>
        OfferQuantity,

        /// <summary>
        /// Supplier's product description - stored in OfferProductAnnex.Description
        /// </summary>
        OfferDescription,

        /// <summary>
        /// Generic core product property - stored in ProductEntity.DynamicProperties[key]
        /// </summary>
        ProductDynamic,

        /// <summary>
        /// Generic offer property - stored in OfferProductAnnex.OfferProperties[key]
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
