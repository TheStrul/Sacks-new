using SacksDataLayer.Models;

namespace SacksDataLayer.Configuration
{
    /// <summary>
    /// Description extractor removed — this stub preserves API but no longer performs extraction.
    /// All description-based extraction has been intentionally disabled. Use supplier-specific
    /// column transformations (extractpattern, extractsizeunits) in `supplier-formats.json` instead.
    /// </summary>
    public sealed class ConfigurationDescriptionPropertyExtractor
    {
        public ConfigurationDescriptionPropertyExtractor(ProductPropertyNormalizationConfiguration configuration)
        {
            // No-op: extractor disabled. Keep constructor to preserve compatibility but do nothing.
        }

        public System.Collections.Generic.Dictionary<string, object?> ExtractPropertiesFromDescription(string description)
        {
            return new System.Collections.Generic.Dictionary<string, object?>();
        }

        public DescriptionExtractionOutcome ExtractWithLeftOver(string description)
        {
            return new DescriptionExtractionOutcome { Source = description, KeyValues = new System.Collections.Generic.Dictionary<string,string>(), LeftOver = string.IsNullOrWhiteSpace(description) ? string.Empty : description };
        }

        public string NormalizeValue(string propertyKey, string value) => value;
    }
}
