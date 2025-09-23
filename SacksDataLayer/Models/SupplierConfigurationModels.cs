using System.Text.Json.Serialization;

namespace SacksDataLayer.FileProcessing.Configuration
{
    /// <summary>
    /// Root configuration containing all supplier configurations
    /// </summary>
    public class SuppliersConfiguration
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = "2.1";

        [JsonPropertyName("metadata")]
        public string FullPath { get; set; } = ".";
        
        [JsonPropertyName("suppliers")]
        public List<SupplierConfiguration> Suppliers { get; set; } = new();

        [JsonPropertyName("lookups")]
        public Dictionary<string, Dictionary<string, string>> Lookups { get; set; } = new();

    }

    /// <summary>
    /// Configuration for a specific supplier
    /// </summary>
    public class SupplierConfiguration
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        public string Currency { get; set; } = "$";

        [JsonPropertyName("parserConfig")]
        public ParsingEngine.ParserConfig? ParserConfig { get; set; }

        [JsonPropertyName("fileStructure")]
        public FileStructureConfiguration FileStructure { get; set; } = new();

        [JsonPropertyName("subtitleHandling")]
        public SubtitleRowHandlingConfiguration? SubtitleHandling { get; set; }

        /// <summary>
        /// Reference to parent configuration (not serialized)
        /// </summary>
        [JsonIgnore]
        public SuppliersConfiguration? ParentConfiguration { get; set; }
    }

    /// <summary>
    /// File structure configuration
    /// </summary>
    public class FileStructureConfiguration
    {
        [JsonPropertyName("dataStartRowIndex")]
        public int DataStartRowIndex { get; set; } = 5; // 1-based index (Excel row number)

        [JsonPropertyName("expectedColumnCount")]
        public int ExpectedColumnCount { get; set; } = 15;

        [JsonPropertyName("headerRowIndex")]
        public int HeaderRowIndex { get; set; } = 4; // 1-based index

        [JsonPropertyName("detection")]
        public DetectionConfiguration? Detection { get; set; }

    }

    /// <summary>
    /// Configuration for handling subtitle rows in Excel files
    /// </summary>
    public class SubtitleRowHandlingConfiguration
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonPropertyName("action")]
        public string Action { get; set; } = "parse"; // "skip" or "parse"

        [JsonPropertyName("detectionRules")]
        public List<SubtitleDetectionRule> DetectionRules { get; set; } = new();

        [JsonPropertyName("fallbackAction")]
        public string FallbackAction { get; set; } = "skip"; // "skip" or "ignore"
    }

    /// <summary>
    /// Rule for detecting and parsing specific types of subtitle rows
    /// </summary>
    public class SubtitleDetectionRule
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("detectionMethod")]
        public string DetectionMethod { get; set; } = "columnCount"; // "columnCount", "pattern", "hybrid"

        [JsonPropertyName("expectedColumnCount")]
        public int ExpectedColumnCount { get; set; } = 1;

        [JsonPropertyName("applyToSubsequentRows")]
        public bool ApplyToSubsequentRows { get; set; } = true;

        [JsonPropertyName("validationPatterns")]
        public List<string> ValidationPatterns { get; set; } = new();
    }

    /// <summary>
    /// Supplier file detection configuration
    /// </summary>
    public class DetectionConfiguration
    {
        [JsonPropertyName("fileNamePatterns")]
        public List<string> FileNamePatterns { get; set; } = new();

        // Future fields like priority, content-based rules etc. can be added here
    }
}