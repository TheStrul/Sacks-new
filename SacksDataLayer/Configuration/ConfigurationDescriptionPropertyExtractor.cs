using System.Text.RegularExpressions;
using SacksDataLayer.Configuration;

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
                var extractedValue = ExtractPropertyValue(description, patternGroup.Value);
                if (!string.IsNullOrEmpty(extractedValue))
                {
                    extractedProperties[patternGroup.Key] = extractedValue;
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
