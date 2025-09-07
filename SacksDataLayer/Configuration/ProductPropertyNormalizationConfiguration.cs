using System.Text.Json;
using System.Text.Json.Serialization;
using SacksDataLayer.Models;

namespace SacksDataLayer.Configuration
{
    /// <summary>
    /// Configuration for property key/value normalization and extraction patterns
    /// Replaces hardcoded mappings in PropertyNormalizer and DescriptionPropertyExtractor
    /// </summary>
    public class ProductPropertyNormalizationConfiguration
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0";

        [JsonPropertyName("productType")]
        public string ProductType { get; set; } = string.Empty;

        [JsonPropertyName("keyMappings")]
        public Dictionary<string, string> KeyMappings { get; set; } = new();

        [JsonPropertyName("valueMappings")]
        public Dictionary<string, Dictionary<string, string>> ValueMappings { get; set; } = new();

        [JsonPropertyName("extractionPatterns")]
        public Dictionary<string, List<PropertyExtractionPattern>> ExtractionPatterns { get; set; } = new();

        [JsonPropertyName("filterableProperties")]
        public List<FilterablePropertyDefinition> FilterableProperties { get; set; } = new();

        [JsonPropertyName("sortableProperties")]
        public List<SortablePropertyDefinition> SortableProperties { get; set; } = new();
    }

    /// <summary>
    /// Pattern for extracting properties from unstructured text (descriptions)
    /// </summary>
    public class PropertyExtractionPattern
    {
        [JsonPropertyName("pattern")]
        public string Pattern { get; set; } = string.Empty;

        [JsonPropertyName("groupIndex")]
        public int GroupIndex { get; set; } = 0;

        [JsonPropertyName("priority")]
        public int Priority { get; set; } = 1;

        [JsonPropertyName("transformation")]
        public string? Transformation { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }

    /// <summary>
    /// Definition of a filterable property with its possible values
    /// </summary>
    public class FilterablePropertyDefinition
    {
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;

        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; } = string.Empty;

        [JsonPropertyName("dataType")]
        public PropertyDataType DataType { get; set; } = PropertyDataType.String;

        [JsonPropertyName("possibleValues")]
        public List<string> PossibleValues { get; set; } = new();

        [JsonPropertyName("isRangeFilter")]
        public bool IsRangeFilter { get; set; } = false;

        [JsonPropertyName("displayOrder")]
        public int DisplayOrder { get; set; } = 0;
    }

    /// <summary>
    /// Definition of a sortable property
    /// </summary>
    public class SortablePropertyDefinition
    {
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;

        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; } = string.Empty;

        [JsonPropertyName("dataType")]
        public PropertyDataType DataType { get; set; } = PropertyDataType.String;

        [JsonPropertyName("defaultDirection")]
        public SortDirection DefaultDirection { get; set; } = SortDirection.Ascending;

        [JsonPropertyName("displayOrder")]
        public int DisplayOrder { get; set; } = 0;
    }

    /// <summary>
    /// Manager for property normalization configurations
    /// </summary>
    public class PropertyNormalizationConfigurationManager
    {
        private static readonly JsonSerializerOptions DefaultOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };

        private readonly Dictionary<string, ProductPropertyNormalizationConfiguration> _configurations = new();

        /// <summary>
        /// Loads normalization configuration from JSON file
        /// </summary>
        public async Task<ProductPropertyNormalizationConfiguration> LoadConfigurationAsync(string fullPath, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fullPath);
            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"Configuration file not found: {fullPath}");

            var json = await File.ReadAllTextAsync(fullPath, cancellationToken);
            var config = JsonSerializer.Deserialize<ProductPropertyNormalizationConfiguration>(json, DefaultOptions)
                ?? throw new InvalidOperationException($"Failed to deserialize normalization configuration from {fullPath}");

            _configurations[config.ProductType] = config;
            return config;
        }

        /// <summary>
        /// Gets normalization configuration for a specific product type
        /// </summary>
        public ProductPropertyNormalizationConfiguration? GetConfiguration(string productType)
        {
            return _configurations.TryGetValue(productType, out var config) ? config : null;
        }

        /// <summary>
        /// Saves normalization configuration to JSON file
        /// </summary>
        public async Task SaveConfigurationAsync(ProductPropertyNormalizationConfiguration configuration, string filePath, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

            var json = JsonSerializer.Serialize(configuration, DefaultOptions);
            
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(filePath, json, cancellationToken);
        }

        /// <summary>
        /// Creates a default configuration for perfume products
        /// </summary>
        public ProductPropertyNormalizationConfiguration CreateDefaultPerfumeConfiguration()
        {
            return new ProductPropertyNormalizationConfiguration
            {
                ProductType = "Perfume",
                KeyMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                ValueMappings = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase),
                ExtractionPatterns = new Dictionary<string, List<PropertyExtractionPattern>>(StringComparer.OrdinalIgnoreCase),
                FilterableProperties = new List<FilterablePropertyDefinition>(),
                SortableProperties = new List<SortablePropertyDefinition>()
            };
        }
    }
}
