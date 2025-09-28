using System.Text.Json.Serialization;

using ParsingEngine;

namespace SacksDataLayer.FileProcessing.Configuration
{
    /// <summary>
    /// Root configuration containing all supplier configurations
    /// </summary>
    public sealed class SuppliersConfiguration : ISuppliersConfiguration
    {
        public string Version { get; set; } = "2.1";

        public string FullPath { get; set; } = ".";
        
        public List<SupplierConfiguration> Suppliers { get; set; } = new();

        public Dictionary<string, Dictionary<string, string>> Lookups { get; set; } = new();

        public Task Save()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Validate the loaded SuppliersConfiguration and return a list of validation error messages.
        /// If the returned list is empty the configuration is considered valid.
        /// </summary>
        public IList<string> ValidateConfiguration()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(this.Version))
                errors.Add("Missing or empty 'version' in supplier configuration");

            // Lookups
            if (this.Lookups == null)
            {
                errors.Add("Lookups dictionary is null");
            }
            else
            {
                foreach (var tbl in this.Lookups)
                {
                    if (string.IsNullOrWhiteSpace(tbl.Key))
                    {
                        errors.Add("Lookup table has empty name");
                        continue;
                    }
                    if (tbl.Value == null)
                    {
                        errors.Add($"Lookup table '{tbl.Key}' has null entries dictionary");
                        continue;
                    }
                    foreach (var kv in tbl.Value)
                    {
                        if (kv.Key == null) errors.Add($"Lookup '{tbl.Key}' contains a null key");
                        if (kv.Value == null) errors.Add($"Lookup '{tbl.Key}' contains a null value for key '{kv.Key}'");
                    }
                }
            }

            // Suppliers
            if (this.Suppliers == null)
            {
                errors.Add("Suppliers list is null");
            }
            else if (this.Suppliers.Count == 0)
            {
                errors.Add("No suppliers defined in configuration");
            }
            else
            {
                for (int i = 0; i < this.Suppliers.Count; i++)
                {
                    var s = this.Suppliers[i];
                    // Ensure suppliers ParentConfiguration references are set (defensive)
                    s.ParentConfiguration = this;
                    // merge lookups into each supplier's parser config
                    s.ParserConfig!.DoMergeLoookUpTables(this.Lookups!);
                    var idx = i + 1;
                    if (s == null)
                    {
                        errors.Add($"Supplier at index {idx} is null");
                        continue;
                    }
                    if (string.IsNullOrWhiteSpace(s.Name))
                        errors.Add($"Supplier at index {idx} has missing or empty name");

                    if (s.FileStructure == null)
                    {
                        errors.Add($"Supplier '{s.Name}' has null fileStructure");
                    }
                    else
                    {
                        if (s.FileStructure.DataStartRowIndex < 1)
                            errors.Add($"Supplier '{s.Name}' has invalid dataStartRowIndex (must be >= 1)");
                        if (s.FileStructure.HeaderRowIndex < 1)
                            errors.Add($"Supplier '{s.Name}' has invalid headerRowIndex (must be >= 1)");
                        if (s.FileStructure.ExpectedColumnCount < 1)
                            errors.Add($"Supplier '{s.Name}' has invalid expectedColumnCount (must be >= 1)");
                        if (s.FileStructure.Detection != null && (s.FileStructure.Detection.FileNamePatterns == null || s.FileStructure.Detection.FileNamePatterns.Count == 0))
                            errors.Add($"Supplier '{s.Name}' has fileStructure.detection but no fileNamePatterns defined");
                    }

                    if (s.SubtitleHandling != null)
                    {
                        var sh = s.SubtitleHandling;
                        if (sh.Action != "parse" && sh.Action != "skip")
                            errors.Add($"Supplier '{s.Name}' has invalid subtitleHandling.action '{sh.Action}' (allowed: 'parse','skip')");
                        if (sh.DetectionRules == null) sh.DetectionRules = new List<SubtitleDetectionRule>();
                        for (int r = 0; r < sh.DetectionRules.Count; r++)
                        {
                            var rule = sh.DetectionRules[r];
                            if (rule == null)
                            {
                                errors.Add($"Supplier '{s.Name}' subtitle detection rule at index {r} is null");
                                continue;
                            }
                            if (rule.ExpectedColumnCount < 1)
                                errors.Add($"Supplier '{s.Name}' subtitle detection rule '{rule.Name}' has invalid expectedColumnCount");
                            if (rule.DetectionMethod != "columnCount" && rule.DetectionMethod != "pattern" && rule.DetectionMethod != "hybrid")
                                errors.Add($"Supplier '{s.Name}' subtitle detection rule '{rule.Name}' has invalid detectionMethod '{rule.DetectionMethod}'");
                        }
                    }

                    // ParserConfig light validation
                    if (s.ParserConfig == null)
                    {
                        // It's acceptable in some cases but we flag as warning-level by adding an error
                        errors.Add($"Supplier '{s.Name}' missing parserConfig");
                    }
                    else
                    {
                        if (s.ParserConfig.ColumnRules == null || s.ParserConfig.ColumnRules.Count == 0)
                        {
                            errors.Add($"Supplier '{s.Name}' parserConfig contains no columns");
                        }
                        else
                        {
                            int index = 0;
                            foreach (KeyValuePair<string,RuleConfig> keyVal in s.ParserConfig.ColumnRules)
                            {
                                index++;
                                
                                if (string.IsNullOrEmpty(keyVal.Key))
                                    errors.Add($"Supplier '{s.Name}' parserConfig contains a column with empty 'column' field at index {index}");
                                if (keyVal.Value== null)
                                    errors.Add($"Supplier '{s.Name}' parserConfig column '{keyVal.Key}' missing rule");
                                else if (keyVal.Value.Actions == null || keyVal.Value.Actions.Count == 0)
                                    errors.Add($"Supplier '{s.Name}' parserConfig column '{keyVal.Key}' rule contains no actions");
                                else
                                {
                                    for (int aidx = 0; aidx < keyVal.Value.Actions.Count; aidx++)
                                    {
                                        var act = keyVal.Value.Actions[aidx];
                                        if (act == null)
                                        {
                                            errors.Add($"Supplier '{s.Name}' column '{keyVal.Key}' has null action at index {aidx}");
                                            continue;
                                        }
                                        if (string.IsNullOrWhiteSpace(act.Op))
                                            errors.Add($"Supplier '{s.Name}' column '{keyVal.Key}' action at index {aidx} missing Op");
                                        // If map op, ensure table parameter exists
                                        if (string.Equals(act.Op, "map", StringComparison.OrdinalIgnoreCase) || string.Equals(act.Op, "mapping", StringComparison.OrdinalIgnoreCase))
                                        {
                                            if (act.Parameters == null || !act.Parameters.ContainsKey("table") || string.IsNullOrWhiteSpace(act.Parameters["table"]))
                                                errors.Add($"Supplier '{s.Name}' column '{keyVal.Key}' mapping action at index {aidx} missing Parameters.table");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return errors;
        }
    }

    /// <summary>
    /// Configuration for a specific supplier
    /// </summary>
    public class SupplierConfiguration
    {
        public string Name { get; set; } = string.Empty;
        
        public string Currency { get; set; } = "$";

        public ParsingEngine.ParserConfig? ParserConfig { get; set; }

        public FileStructureConfiguration FileStructure { get; set; } = new();

        public SubtitleRowHandlingConfiguration? SubtitleHandling { get; set; }

        /// <summary>
        /// Reference to parent configuration (not serialized)
        /// </summary>
        [JsonIgnore]
        public ISuppliersConfiguration? ParentConfiguration { get; set; }
    }

    /// <summary>
    /// File structure configuration
    /// </summary>
    public class FileStructureConfiguration
    {
        public int DataStartRowIndex { get; set; } = 5; // 1-based index (Excel row number)

        public int ExpectedColumnCount { get; set; } = 15;

        public int HeaderRowIndex { get; set; } = 4; // 1-based index

        public DetectionConfiguration? Detection { get; set; }

    }

    /// <summary>
    /// Configuration for handling subtitle rows in Excel files
    /// </summary>
    public class SubtitleRowHandlingConfiguration
    {
        public bool Enabled { get; set; } = true;

        public string Action { get; set; } = "parse"; // "skip" or "parse"

        public List<SubtitleDetectionRule> DetectionRules { get; set; } = new();

       public string FallbackAction { get; set; } = "skip"; // "skip" or "ignore"
    }

    /// <summary>
    /// Rule for detecting and parsing specific types of subtitle rows
    /// </summary>
    public class SubtitleDetectionRule
    {
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string DetectionMethod { get; set; } = "columnCount"; // "columnCount", "pattern", "hybrid"

        public int ExpectedColumnCount { get; set; } = 1;

        public bool ApplyToSubsequentRows { get; set; } = true;

        public List<string> ValidationPatterns { get; set; } = new();
    }

    /// <summary>
    /// Supplier file detection configuration
    /// </summary>
    public class DetectionConfiguration
    {
        public List<string> FileNamePatterns { get; set; } = new();
    }
}