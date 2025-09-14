using System.Text.RegularExpressions;

namespace SacksDataLayer.Configuration
{
    /// <summary>
    /// Configuration-driven normalizer that replaces hardcoded property mappings
    /// Uses ProductPropertyNormalizationConfiguration for all mappings
    /// </summary>
    public class ConfigurationPropertyNormalizer
    {
        private readonly ProductPropertyNormalizationConfiguration _configuration;

        public ProductPropertyNormalizationConfiguration Configuration => _configuration;

        public ConfigurationPropertyNormalizer(ProductPropertyNormalizationConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// Normalizes a property value to standard form using configuration
        /// </summary>
        /// <param name="key">Normalized property key</param>
        /// <param name="value">Original property value</param>
        /// <returns>Normalized value</returns>
        public string NormalizeValue(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;

            var normalizedValue = value.Trim().ToLowerInvariant();

            // Strip punctuation/symbols and collapse whitespace for robust matching
            normalizedValue = Regex.Replace(normalizedValue, "[\\p{P}\\p{S}]+", " ");
            normalizedValue = Regex.Replace(normalizedValue, "\\s+", " ").Trim();

            if (_configuration.ValueMappings.TryGetValue(key, out var valueMap) &&
                valueMap.TryGetValue(normalizedValue, out var standardValue))
            {
                return standardValue;
            }

            return value; // Return original if no mapping found
        }

        /// <summary>
        /// Normalizes all properties in a dictionary
        /// </summary>
        /// <param name="properties">Original properties</param>
        /// <returns>Normalized properties</returns>
        public Dictionary<string, object?> NormalizeProperties(Dictionary<string, object?> properties)
        {
            ArgumentNullException.ThrowIfNull(properties);
            
            var normalized = new Dictionary<string, object?>();

            foreach (var kvp in properties)
            {
                var normalizedKey = kvp.Key;
                var normalizedValue = kvp.Value?.ToString();

                if (!string.IsNullOrWhiteSpace(normalizedValue))
                {
                    normalizedValue = NormalizeValue(normalizedKey, normalizedValue);
                }

                normalized[normalizedKey] = normalizedValue;
            }

            return normalized;
        }
    }
}
