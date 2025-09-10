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
        /// Normalizes a property key to standard form using configuration
        /// </summary>
        /// <param name="key">Original property key</param>
        /// <returns>Normalized key</returns>
        public string NormalizeKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return key;

            var normalizedKey = key.Trim().ToLowerInvariant();
            
            return _configuration.KeyMappings.TryGetValue(normalizedKey, out var standardKey) 
                ? standardKey 
                : key; // Return original if no mapping found
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
                var normalizedKey = NormalizeKey(kvp.Key);
                var normalizedValue = kvp.Value?.ToString();

                if (!string.IsNullOrWhiteSpace(normalizedValue))
                {
                    normalizedValue = NormalizeValue(normalizedKey, normalizedValue);
                }

                normalized[normalizedKey] = normalizedValue;
            }

            return normalized;
        }

        /// <summary>
        /// Gets all possible normalized values for a given property from configuration
        /// </summary>
        /// <param name="propertyKey">Normalized property key</param>
        /// <returns>List of possible normalized values</returns>
        public List<string> GetPossibleValues(string propertyKey)
        {
            if (_configuration.ValueMappings.TryGetValue(propertyKey, out var valueMap))
            {
                return valueMap.Values.Distinct().ToList();
            }

            // Also check filterable properties configuration
            var filterableProperty = _configuration.FilterableProperties
                .FirstOrDefault(p => p.Key.Equals(propertyKey, StringComparison.OrdinalIgnoreCase));
            
            return filterableProperty?.PossibleValues ?? new List<string>();
        }

        /// <summary>
        /// Gets all normalized property keys from configuration
        /// </summary>
        /// <returns>List of normalized property keys</returns>
        public List<string> GetNormalizedKeys()
        {
            return _configuration.KeyMappings.Values.Distinct().ToList();
        }

        /// <summary>
        /// Gets all original values that normalize to the specified normalized value
        /// </summary>
        /// <param name="normalizedKey">The normalized property key</param>
        /// <param name="normalizedValue">The normalized value to find variations for</param>
        /// <returns>List of all original values that would normalize to this value</returns>
        public List<string> GetOriginalValuesForNormalized(string normalizedKey, string normalizedValue)
        {
            var originalValues = new List<string>();

            if (_configuration.ValueMappings.TryGetValue(normalizedKey, out var valueMap))
            {
                foreach (var kvp in valueMap)
                {
                    if (kvp.Value.Equals(normalizedValue, StringComparison.OrdinalIgnoreCase))
                    {
                        originalValues.Add(kvp.Key);
                    }
                }
            }

            return originalValues;
        }

        /// <summary>
        /// Gets all original keys that normalize to the specified normalized key
        /// </summary>
        /// <param name="normalizedKey">The normalized property key</param>
        /// <returns>List of all original keys that would normalize to this key</returns>
        public List<string> GetOriginalKeysForNormalized(string normalizedKey)
        {
            var originalKeys = new List<string>();

            foreach (var kvp in _configuration.KeyMappings)
            {
                if (kvp.Value.Equals(normalizedKey, StringComparison.OrdinalIgnoreCase))
                {
                    originalKeys.Add(kvp.Key);
                }
            }

            return originalKeys;
        }

        /// <summary>
        /// Gets a complete mapping of normalized values to their original variations
        /// for a specific property key
        /// </summary>
        /// <param name="normalizedKey">The normalized property key</param>
        /// <returns>Dictionary mapping normalized values to lists of original variations</returns>
        public Dictionary<string, List<string>> GetCompleteValueMappingForProperty(string normalizedKey)
        {
            var mappingsByNormalizedValue = new Dictionary<string, List<string>>();

            if (_configuration.ValueMappings.TryGetValue(normalizedKey, out var valueMap))
            {
                foreach (var kvp in valueMap)
                {
                    if (!mappingsByNormalizedValue.ContainsKey(kvp.Value))
                    {
                        mappingsByNormalizedValue[kvp.Value] = new List<string>();
                    }
                    mappingsByNormalizedValue[kvp.Value].Add(kvp.Key);
                }
            }

            return mappingsByNormalizedValue;
        }

        /// <summary>
        /// Creates a ConfigurationPropertyNormalizer from configuration file
        /// </summary>
        public static async Task<ConfigurationPropertyNormalizer> CreateFromConfigurationAsync(
            string configurationFilePath, 
            CancellationToken cancellationToken = default)
        {
            var manager = new PropertyNormalizationConfigurationManager();
            var config = await manager.LoadConfigurationAsync(configurationFilePath, cancellationToken);
            return new ConfigurationPropertyNormalizer(config);
        }
    }
}
