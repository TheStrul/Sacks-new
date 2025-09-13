using System.Text.Json.Serialization;

namespace SacksDataLayer.Models
{
    /// <summary>
    /// Represents the result of parsing an offer description into structured key/values
    /// and the leftover (unparsed) portion of the description.
    /// </summary>
    public class DescriptionExtractionOutcome
    {
        /// <summary>
        /// Extracted key/value pairs (already normalized)
        /// </summary>
        [JsonPropertyName("keyValues")]
        public Dictionary<string, string> KeyValues { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The remaining text after removing all recognized parts.
        /// </summary>
        [JsonPropertyName("leftOver")]
        public string LeftOver { get; set; } = string.Empty;

        /// <summary>
        /// The original source description (optional, for traceability)
        /// </summary>
        [JsonPropertyName("source")] 
        public string? Source { get; set; }
    }
}
