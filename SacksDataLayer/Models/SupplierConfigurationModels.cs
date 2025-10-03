using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using System.IO;
using System.Text.RegularExpressions;

using ParsingEngine;

namespace SacksDataLayer.FileProcessing.Configuration
{
    /// <summary>
    /// Root configuration containing all supplier configurations
    /// </summary>
    public sealed class SuppliersConfiguration : ISuppliersConfiguration
    {
        private static readonly JsonSerializerOptions s_jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public string Version { get; set; } = "2.1";

        public string FullPath { get; set; } = ".";
        
        public List<SupplierConfiguration> Suppliers { get; set; } = new();

        public Dictionary<string, Dictionary<string, string>> Lookups { get; set; } = new();

        public async Task Save()
        {
            if (string.IsNullOrWhiteSpace(this.FullPath))
                throw new InvalidOperationException("SuppliersConfiguration.FullPath is not set");

            // Ensure directory exists
            var dir = Path.GetDirectoryName(this.FullPath);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = JsonSerializer.Serialize(this, s_jsonOptions);
            await File.WriteAllTextAsync(this.FullPath, json).ConfigureAwait(false);
        }

        /// <summary>
        /// Validate the loaded SuppliersConfiguration and return a list of validation error messages.
        /// If the returned list is empty the configuration is considered valid.
        /// </summary>
        public IList<string> ValidateConfiguration()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(this.Version))
                errors.Add("Missing or empty 'Version' in supplier configuration");

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

                        // Validate optional assignments
                        if (sh.Assignments != null)
                        {
                            for (int a = 0; a < sh.Assignments.Count; a++)
                            {
                                var m = sh.Assignments[a];
                                if (m == null)
                                {
                                    errors.Add($"Supplier '{s.Name}' subtitle assignment at index {a} is null");
                                    continue;
                                }
                                if (string.IsNullOrWhiteSpace(m.SourceKey))
                                    errors.Add($"Supplier '{s.Name}' subtitle assignment[{a}] missing SourceKey");
                                if (string.IsNullOrWhiteSpace(m.TargetProperty))
                                    errors.Add($"Supplier '{s.Name}' subtitle assignment[{a}] missing TargetProperty");
                                // If LookupTable specified ensure present in merged lookups
                                if (!string.IsNullOrWhiteSpace(m.LookupTable))
                                {
                                    var available = s.ParserConfig?.Lookups ?? new Dictionary<string, Dictionary<string,string>>();
                                    if (!available.ContainsKey(m.LookupTable))
                                        errors.Add($"Supplier '{s.Name}' subtitle assignment[{a}] references unknown lookup table '{m.LookupTable}'");
                                }
                            }
                        }

                        // Validate transforms if present
                        if (sh.Transforms != null)
                        {
                            foreach (var tr in sh.Transforms)
                            {
                                if (tr == null) { errors.Add($"Supplier '{s.Name}' has a null subtitle transform"); continue; }
                                if (string.IsNullOrWhiteSpace(tr.SourceKey)) errors.Add($"Supplier '{s.Name}' subtitle transform missing SourceKey");
                                if (string.IsNullOrWhiteSpace(tr.Mode)) errors.Add($"Supplier '{s.Name}' subtitle transform missing Mode");
                                if (string.Equals(tr.Mode, "regexreplace", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(tr.Pattern))
                                    errors.Add($"Supplier '{s.Name}' subtitle transform regexReplace missing Pattern");
                                if (string.Equals(tr.Mode, "regexreplace", StringComparison.OrdinalIgnoreCase))
                                {
                                    try { _ = new Regex(tr.Pattern!); } catch (Exception ex) { errors.Add($"Supplier '{s.Name}' subtitle transform invalid regex: {ex.Message}"); }
                                }
                            }
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
                                var columnName = keyVal.Key;
                                var ruleCfg = keyVal.Value;
                                if (string.IsNullOrEmpty(columnName))
                                    errors.Add($"Supplier '{s.Name}' parserConfig contains a column with empty 'column' field at index {index}");
                                if (ruleCfg== null)
                                    errors.Add($"Supplier '{s.Name}' parserConfig column '{columnName}' missing rule");
                                else if (ruleCfg.Actions == null || ruleCfg.Actions.Count == 0)
                                    errors.Add($"Supplier '{s.Name}' parserConfig column '{columnName}' rule contains no actions");
                                else
                                {
                                    for (int aidx = 0; aidx < ruleCfg.Actions.Count; aidx++)
                                    {
                                        var act = ruleCfg.Actions[aidx];
                                        if (act == null)
                                        {
                                            errors.Add($"Supplier '{s.Name}' column '{columnName}' has null action at index {aidx}");
                                            continue;
                                        }

                                        // Basic action fields
                                        if (string.IsNullOrWhiteSpace(act.Op))
                                            errors.Add($"Supplier '{s.Name}' column '{columnName}' action at index {aidx} missing Op");

                                        if (string.IsNullOrWhiteSpace(act.Input))
                                            errors.Add($"Supplier '{s.Name}' column '{columnName}' action '{act.Op}' at index {aidx} missing Input");

                                        if (string.IsNullOrWhiteSpace(act.Output) && !string.Equals(act.Op, "noop", StringComparison.OrdinalIgnoreCase))
                                            errors.Add($"Supplier '{s.Name}' column '{columnName}' action '{act.Op}' at index {aidx} missing Output");

                                        // Validate per-op parameters and detect unused ones
                                        ValidateActionParameters(s, columnName, act, aidx, errors);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return errors;
        }

        private static void ValidateActionParameters(SupplierConfiguration supplier, string columnName, ActionConfig act, int actionIndex, List<string> errors)
        {
            if (act == null) return;
            var op = (act.Op ?? string.Empty).Trim();
            var parameters = act.Parameters ?? new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);

            // helper to add error with context
            void AddErr(string msg) => errors.Add($"Supplier '{supplier.Name}' column '{columnName}' action[{actionIndex}] op='{op}': {msg}");

            switch (op.ToLowerInvariant())
            {
                case "assign":
                    // assign uses Input/Output only; no parameters expected
                    if (parameters.Count > 0)
                        AddErr($"unused Parameters: {string.Join(',', parameters.Keys)} (assign expects no parameters)");
                    break;
                case "find":
                    // expected keys: Pattern, Options (optional)
                    if (!parameters.TryGetValue("Pattern", out var pattern) || string.IsNullOrWhiteSpace(pattern))
                    {
                        AddErr("missing Parameters['Pattern'] or Pattern is empty");
                    }
                    else
                    {
                        if (pattern.StartsWith("Lookup:", StringComparison.OrdinalIgnoreCase))
                        {
                            var tbl = pattern[("Lookup:").Length..].Trim();
                            // merged lookups are in supplier.ParserConfig.Lookups
                            var available = supplier.ParserConfig?.Lookups ?? new Dictionary<string, Dictionary<string,string>>();
                            if (!available.ContainsKey(tbl))
                                AddErr($"Pattern references lookup table '{tbl}' which is not present in merged lookups");
                        }
                        else
                        {
                            // Try compiling regex to detect invalid patterns early
                            try
                            {
                                _ = new Regex(pattern);
                            }
                            catch (Exception ex)
                            {
                                AddErr($"invalid regex Pattern: {ex.Message}");
                            }
                        }
                    }
                    // detect unused parameter keys
                    var allowedFind = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Pattern", "Options" };
                    var unusedFind = parameters.Keys.Where(k => !allowedFind.Contains(k)).ToList();
                    if (unusedFind.Count > 0) AddErr($"unused Parameters: {string.Join(',', unusedFind)}");
                    break;
                case "split":
                case "splitbydelimiter":
                    // allowed: Delimiter, ExpectedParts, Strict
                    var allowedSplit = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Delimiter", "ExpectedParts", "Strict" };
                    var unusedSplit = parameters.Keys.Where(k => !allowedSplit.Contains(k)).ToList();
                    if (unusedSplit.Count > 0) AddErr($"unused Parameters: {string.Join(',', unusedSplit)}");
                    break;
                case "map":
                case "mapping":
                    // required: Table
                    if (!parameters.TryGetValue("Table", out var tableName) || string.IsNullOrWhiteSpace(tableName))
                    {
                        AddErr("missing Parameters['Table'] for mapping action");
                    }
                    else
                    {
                        var available = supplier.ParserConfig?.Lookups ?? new Dictionary<string, Dictionary<string,string>>();
                        if (!available.ContainsKey(tableName))
                            AddErr($"mapping Parameters['Table'] references unknown lookup table '{tableName}'");
                    }
                    var allowedMap = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Table", "CaseMode" };
                    var unusedMap = parameters.Keys.Where(k => !allowedMap.Contains(k)).ToList();
                    if (unusedMap.Count > 0) AddErr($"unused Parameters: {string.Join(',', unusedMap)}");
                    break;
                default:
                    // Unknown ops: warn about parameters (can't validate semantics)
                    if (parameters.Count > 0)
                        AddErr($"unknown Op '{op}' - cannot validate parameters but found: {string.Join(',', parameters.Keys)}");
                    break;
            }
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

       // Generic assignments to apply extracted subtitle values to properties
       public List<SubtitleAssignmentMapping> Assignments { get; set; } = new();

       // Optional normalization/transformation rules applied to subtitle values before assignment
       public List<SubtitleTransformRule> Transforms { get; set; } = new();
    }

    /// <summary>
    /// Transformation applied to a subtitle value before mapping
    /// </summary>
    public class SubtitleTransformRule
    {
        public string SourceKey { get; set; } = string.Empty; // e.g., "Brand"
        public string Mode { get; set; } = "regexReplace";   // ignoreEquals | regexReplace | removePrefix
        public string? Pattern { get; set; }                  // required for regexReplace/removePrefix
        public string? Replacement { get; set; } = string.Empty; // used for regexReplace/removePrefix
        public bool IgnoreCase { get; set; } = true;
    }

    /// <summary>
    /// Mapping from extracted subtitle key to a target property, with optional lookup normalization
    /// </summary>
    public class SubtitleAssignmentMapping
    {
        public string SourceKey { get; set; } = string.Empty; // e.g., "Brand", "Type"
        public string TargetProperty { get; set; } = string.Empty; // e.g., "Product.Brand"
        public string? LookupTable { get; set; } // e.g., "Brand", "Type"
        public bool Overwrite { get; set; } = false; // default: only set when missing
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