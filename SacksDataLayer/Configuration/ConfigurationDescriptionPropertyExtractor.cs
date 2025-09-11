using System.Text.RegularExpressions;

namespace SacksDataLayer.Configuration
{
    /// <summary>
    /// Configuration-driven property extractor that replaces hardcoded extraction patterns
    /// Uses ProductPropertyNormalizationConfiguration for all patterns and transformations
    /// </summary>
    public class ConfigurationDescriptionPropertyExtractor
    {
        private readonly ProductPropertyNormalizationConfiguration _configuration;
        private readonly ConfigurationPropertyNormalizer _normalizer;

        public ConfigurationDescriptionPropertyExtractor(ProductPropertyNormalizationConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _normalizer = new ConfigurationPropertyNormalizer(configuration);
        }

        /// <summary>
        /// Extracts properties from description text and returns normalized property dictionary
        /// </summary>
        public Dictionary<string, object?> ExtractPropertiesFromDescription(string description)
        {
            if (string.IsNullOrWhiteSpace(description))
                return new Dictionary<string, object?>();

            var extractedProperties = new Dictionary<string, object?>();

            // Extract each property type using configured patterns
            foreach (var patternGroup in _configuration.ExtractionPatterns)
            {
                var propertyKey = patternGroup.Key;
                var patterns = patternGroup.Value;

                foreach (var pattern in patterns.OrderBy(p => p.Priority))
                {
                    try
                    {
                        var regex = new Regex(pattern.Pattern, RegexOptions.IgnoreCase);
                        var match = regex.Match(description);

                        if (match.Success)
                        {
                            // Check if pattern has multiple output mappings (for size/unit separation)
                            if (pattern.OutputMappings != null && pattern.OutputMappings.Any())
                            {
                                foreach (var mapping in pattern.OutputMappings)
                                {
                                    var outputPropertyKey = mapping.Key;
                                    var groupIndex = mapping.Value;

                                    if (groupIndex < match.Groups.Count)
                                    {
                                        var value = match.Groups[groupIndex].Value?.Trim();
                                        if (!string.IsNullOrEmpty(value))
                                        {
                                            // Apply transformation if specified
                                            if (!string.IsNullOrEmpty(pattern.Transformation))
                                            {
                                                value = ApplyTransformation(value, pattern.Transformation);
                                            }

                                            extractedProperties[outputPropertyKey] = value;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // Standard single property extraction
                                var value = pattern.GroupIndex < match.Groups.Count 
                                    ? match.Groups[pattern.GroupIndex].Value?.Trim()
                                    : match.Value?.Trim();

                                if (!string.IsNullOrEmpty(value))
                                {
                                    // Apply transformation if specified
                                    if (!string.IsNullOrEmpty(pattern.Transformation))
                                    {
                                        value = ApplyTransformation(value, pattern.Transformation);
                                    }

                                    extractedProperties[propertyKey] = value;
                                }
                            }

                            // Break after first successful match for this property group
                            break;
                        }
                    }
                    catch (ArgumentException)
                    {
                        // Invalid regex pattern, skip
                        continue;
                    }
                }
            }

            // Normalize the extracted properties using configuration-based normalizer
            return _normalizer.NormalizeProperties(extractedProperties);
        }

        /// <summary>
        /// Test method to extract and display all matches for debugging purposes
        /// </summary>
        public Dictionary<string, List<string>> ExtractAllMatches(string description)
        {
            if (string.IsNullOrWhiteSpace(description))
                return new Dictionary<string, List<string>>();

            var allMatches = new Dictionary<string, List<string>>();

            foreach (var patternGroup in _configuration.ExtractionPatterns)
            {
                var matches = new List<string>();
                
                foreach (var pattern in patternGroup.Value.OrderBy(p => p.Priority))
                {
                    try
                    {
                        var regex = new Regex(pattern.Pattern, RegexOptions.IgnoreCase);
                        var regexMatches = regex.Matches(description);
                        
                        foreach (Match match in regexMatches)
                        {
                            var value = pattern.GroupIndex < match.Groups.Count 
                                ? match.Groups[pattern.GroupIndex].Value 
                                : match.Value;
                            
                            if (!string.IsNullOrWhiteSpace(value))
                            {
                                matches.Add($"Pattern: {pattern.Pattern} -> {value}");
                            }
                        }
                    }
                    catch (ArgumentException)
                    {
                        // Invalid regex pattern, skip
                        matches.Add($"Invalid pattern: {pattern.Pattern}");
                    }
                }

                if (matches.Any())
                {
                    allMatches[patternGroup.Key] = matches;
                }
            }

            return allMatches;
        }

        /// <summary>
        /// Extracts a specific property value using multiple patterns with priority order
        /// </summary>
        private string? ExtractPropertyValue(string description, List<PropertyExtractionPattern> patterns)
        {
            foreach (var pattern in patterns.OrderBy(p => p.Priority))
            {
                try
                {
                    var regex = new Regex(pattern.Pattern, RegexOptions.IgnoreCase);
                    var match = regex.Match(description);
                    
                    if (match.Success)
                    {
                        var value = pattern.GroupIndex < match.Groups.Count 
                            ? match.Groups[pattern.GroupIndex].Value 
                            : match.Value;
                        
                        if (pattern.Transformation != null)
                        {
                            value = ApplyTransformation(value, pattern.Transformation);
                        }

                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            return value.Trim();
                        }
                    }
                }
                catch (ArgumentException)
                {
                    // Invalid regex pattern, skip
                    continue;
                }
            }
            return null;
        }

        /// <summary>
        /// Applies transformation to extracted value based on configuration
        /// </summary>
        private string ApplyTransformation(string value, string transformation)
        {
            if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(transformation))
                return value;

            return transformation.ToLowerInvariant() switch
            {
                "uppercase" => value.ToUpperInvariant(),
                "lowercase" => value.ToLowerInvariant(),
                "trim" => value.Trim(),
                "removeml" => value.Replace("ml", "", StringComparison.OrdinalIgnoreCase).Trim(),
                "removespaces" => value.Replace(" ", ""),
                "capitalize" => char.ToUpperInvariant(value[0]) + value[1..].ToLowerInvariant(),
                _ => value
            };
        }

        /// <summary>
        /// Normalizes a single property value using the configured value mappings.
        /// This is a small bridge so other normalizers can apply valueMappings when explicitly requested.
        /// </summary>
        /// <param name="propertyKey">Normalized property key (e.g. "COO", "Gender")</param>
        /// <param name="value">Original value</param>
        /// <returns>Normalized value if mapping exists; original value otherwise</returns>
        public string NormalizeValue(string propertyKey, string value)
        {
            if (string.IsNullOrWhiteSpace(propertyKey) || string.IsNullOrWhiteSpace(value))
                return value;

            try
            {
                return _normalizer.NormalizeValue(propertyKey, value);
            }
            catch
            {
                // On any error, return original value to avoid breaking processing
                return value;
            }
        }

        /// <summary>
        /// Creates a ConfigurationDescriptionPropertyExtractor from configuration file
        /// </summary>
        public static async Task<ConfigurationDescriptionPropertyExtractor> CreateFromConfigurationAsync(
            string configurationFilePath, 
            CancellationToken cancellationToken = default)
        {
            var manager = new PropertyNormalizationConfigurationManager();
            var config = await manager.LoadConfigurationAsync(configurationFilePath, cancellationToken);
            return new ConfigurationDescriptionPropertyExtractor(config);
        }
    }
}
