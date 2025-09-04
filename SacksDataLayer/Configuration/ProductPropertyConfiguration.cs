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
                .Where(p => p.Classification == PropertyClassificationType.CoreProduct)
                .OrderBy(p => p.DisplayOrder)
                .ToList();
        }

        /// <summary>
        /// Gets all properties classified as offer properties
        /// </summary>
        public List<ProductPropertyDefinition> GetOfferProperties()
        {
            return Properties.Values
                .Where(p => p.Classification == PropertyClassificationType.Offer)
                .OrderBy(p => p.DisplayOrder)
                .ToList();
        }

        /// <summary>
        /// Gets all filterable properties
        /// </summary>
        public List<ProductPropertyDefinition> GetFilterableProperties()
        {
            return Properties.Values
                .Where(p => p.IsFilterable)
                .OrderBy(p => p.DisplayOrder)
                .ToList();
        }

        /// <summary>
        /// Gets all sortable properties
        /// </summary>
        public List<ProductPropertyDefinition> GetSortableProperties()
        {
            return Properties.Values
                .Where(p => p.IsSortable)
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

        [JsonPropertyName("dataType")]
        public PropertyDataType DataType { get; set; } = PropertyDataType.String;

        [JsonPropertyName("classification")]
        public PropertyClassificationType Classification { get; set; } = PropertyClassificationType.CoreProduct;

        [JsonPropertyName("isFilterable")]
        public bool IsFilterable { get; set; } = true;

        [JsonPropertyName("isSortable")]
        public bool IsSortable { get; set; } = true;

        [JsonPropertyName("isRequired")]
        public bool IsRequired { get; set; } = false;

        [JsonPropertyName("displayOrder")]
        public int DisplayOrder { get; set; } = 0;

        [JsonPropertyName("defaultValue")]
        public object? DefaultValue { get; set; }

        [JsonPropertyName("allowedValues")]
        public List<string> AllowedValues { get; set; } = new();

        [JsonPropertyName("aliases")]
        public List<string> Aliases { get; set; } = new();

        [JsonPropertyName("format")]
        public string? Format { get; set; }

        [JsonPropertyName("unit")]
        public string? Unit { get; set; }

        [JsonPropertyName("category")]
        public string? Category { get; set; }
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
    /// Property classification types
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PropertyClassificationType
    {
        /// <summary>
        /// Core product properties that define the product itself
        /// Stored in ProductEntity.DynamicProperties
        /// </summary>
        CoreProduct,

        /// <summary>
        /// Offer-specific properties that vary by supplier/offer
        /// Stored in OfferProductEntity.ProductProperties
        /// </summary>
        Offer
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
