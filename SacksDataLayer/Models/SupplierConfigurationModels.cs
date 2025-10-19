using System.Text.RegularExpressions;
using System.Text.Json;
using System.Text.Json.Serialization;

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
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public string Version { get; set; } = "2.1";
        public string FullPath { get; set; } = ".";
        [JsonIgnore]
        public List<SupplierConfiguration> Suppliers { get; set; } = new();
        public Dictionary<string, Dictionary<string, string>> Lookups { get; set; } = new();

        public async Task Save()
        {
            if (string.IsNullOrWhiteSpace(this.FullPath))
                throw new InvalidOperationException("SuppliersConfiguration.FullPath is not set");
            var dir = Path.GetDirectoryName(this.FullPath);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            var json = JsonSerializer.Serialize(this, s_jsonOptions);
            await File.WriteAllTextAsync(this.FullPath, json).ConfigureAwait(false);
        }

        /// <summary>
        /// Apply values from another configuration into this instance, updating objects in-place
        /// to keep existing references valid.
        /// </summary>
        public void ApplyFrom(SuppliersConfiguration source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            // Update version and path
            this.Version = source.Version;
            this.FullPath = string.IsNullOrWhiteSpace(source.FullPath) ? this.FullPath : source.FullPath;

            // Merge top-level lookups in-place to preserve dictionary reference
            InPlaceMergeLookups(this.Lookups, source.Lookups);

            // Merge suppliers by Name (case-insensitive)
            var comparer = StringComparer.OrdinalIgnoreCase;
            var map = new Dictionary<string, SupplierConfiguration>(comparer);
            foreach (var s in this.Suppliers)
            {
                if (!string.IsNullOrWhiteSpace(s?.Name) && !map.ContainsKey(s.Name))
                    map[s.Name] = s;
            }

            // Update or add suppliers
            foreach (var incoming in source.Suppliers)
            {
                if (incoming == null || string.IsNullOrWhiteSpace(incoming.Name)) continue;

                if (map.TryGetValue(incoming.Name, out var existing))
                {
                    existing.ParentConfiguration = this;
                    existing.UpdateFrom(incoming, this.Lookups);
                }
                else
                {
                    // Ensure parent reference + merge lookups for parser
                    incoming.ParentConfiguration = this;
                    incoming.ParserConfig?.DoMergeLoookUpTables(this.Lookups);
                    this.Suppliers.Add(incoming);
                    map[incoming.Name] = incoming;
                }
            }

            // Remove suppliers that no longer exist
            for (int i = this.Suppliers.Count - 1; i >= 0; i--)
            {
                var s = this.Suppliers[i];
                if (s == null || !source.Suppliers.Any(ns => string.Equals(ns?.Name, s.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    this.Suppliers.RemoveAt(i);
                }
            }
        }

        private static void InPlaceMergeLookups(Dictionary<string, Dictionary<string, string>> target, Dictionary<string, Dictionary<string, string>> source)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (source == null) source = new(StringComparer.OrdinalIgnoreCase);

            // Ensure case-insensitive for outer dictionary
            if (!(target.Comparer is StringComparer sc && sc.Equals(StringComparer.OrdinalIgnoreCase)))
            {
                var copy = new Dictionary<string, Dictionary<string, string>>(target, StringComparer.OrdinalIgnoreCase);
                target.Clear();
                foreach (var kv in copy) target[kv.Key] = kv.Value;
            }

            // Remove tables not present in source
            var toRemove = target.Keys.Where(k => !source.ContainsKey(k)).ToList();
            foreach (var k in toRemove) target.Remove(k);

            // Merge or add tables
            foreach (var tbl in source)
            {
                var tableName = tbl.Key;
                var incoming = tbl.Value ?? new Dictionary<string, string>();

                if (!target.TryGetValue(tableName, out var destInner))
                {
                    target[tableName] = new Dictionary<string, string>(incoming, StringComparer.OrdinalIgnoreCase);
                }
                else
                {
                    // Ensure case-insensitive inner dictionary
                    if (!(destInner.Comparer is StringComparer sci && sci.Equals(StringComparer.OrdinalIgnoreCase)))
                    {
                        destInner = new Dictionary<string, string>(destInner, StringComparer.OrdinalIgnoreCase);
                        target[tableName] = destInner;
                    }

                    // Remove keys absent in source
                    var innerRemove = destInner.Keys.Where(k => !incoming.ContainsKey(k)).ToList();
                    foreach (var k in innerRemove) destInner.Remove(k);

                    // Upsert all incoming
                    foreach (var kv in incoming)
                    {
                        if (kv.Key == null) continue;
                        destInner[kv.Key] = kv.Value;
                    }
                }
            }
        }

        /// <summary>
        /// Validate the loaded SuppliersConfiguration and return a list of validation error messages.
        /// If the returned list is empty the configuration is considered valid.
        /// </summary>
        public IList<string> ValidateConfiguration()
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(this.Version)) errors.Add("Missing or empty 'Version' in supplier configuration");

            if (this.Lookups == null)
            {
                errors.Add("Lookups dictionary is null");
            }
            else
            {
                foreach (var tbl in this.Lookups)
                {
                    if (string.IsNullOrWhiteSpace(tbl.Key)) { errors.Add("Lookup table has empty name"); continue; }
                    if (tbl.Value == null) { errors.Add($"Lookup table '{tbl.Key}' has null entries dictionary"); continue; }
                    foreach (var kv in tbl.Value)
                    {
                        if (kv.Key == null) errors.Add($"Lookup '{tbl.Key}' contains a null key");
                        if (kv.Value == null) errors.Add($"Lookup '{tbl.Key}' contains a null value for key '{kv.Key}'");
                    }
                }
            }

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
                    s.ParentConfiguration = this;
                    s.ParserConfig!.DoMergeLoookUpTables(this.Lookups!);
                    var idx = i + 1;
                    if (s == null) { errors.Add($"Supplier at index {idx} is null"); continue; }
                    if (string.IsNullOrWhiteSpace(s.Name)) errors.Add($"Supplier at index {idx} has missing or empty name");

                    if (s.FileStructure == null)
                    {
                        errors.Add($"Supplier '{s.Name}' has null fileStructure");
                    }
                    else
                    {
                        if (s.FileStructure.DataStartRowIndex < 1) errors.Add($"Supplier '{s.Name}' has invalid dataStartRowIndex (must be >= 1)");
                        if (s.FileStructure.HeaderRowIndex < 1) errors.Add($"Supplier '{s.Name}' has invalid headerRowIndex (must be >= 1)");
                        if (s.FileStructure.ExpectedColumnCount < 1) errors.Add($"Supplier '{s.Name}' has invalid expectedColumnCount (must be >= 1)");
                        if (s.FileStructure.Detection != null && (s.FileStructure.Detection.FileNamePatterns == null || s.FileStructure.Detection.FileNamePatterns.Count == 0))
                            errors.Add($"Supplier '{s.Name}' has fileStructure.detection but no fileNamePatterns defined");
                    }

                    if (s.SubtitleHandling != null)
                    {
                        var sh = s.SubtitleHandling;
                        if (sh.Action != "parse" && sh.Action != "skip") errors.Add($"Supplier '{s.Name}' has invalid subtitleHandling.action '{sh.Action}' (allowed: 'parse','skip')");
                        if (sh.DetectionRules == null) sh.DetectionRules = new List<SubtitleDetectionRule>();
                        for (int r = 0; r < sh.DetectionRules.Count; r++)
                        {
                            var rule = sh.DetectionRules[r];
                            if (rule == null) { errors.Add($"Supplier '{s.Name}' subtitle detection rule at index {r} is null"); continue; }
                            if (rule.ExpectedColumnCount < 1) errors.Add($"Supplier '{s.Name}' subtitle detection rule '{rule.Name}' has invalid expectedColumnCount");
                            if (rule.DetectionMethod != "columnCount" && rule.DetectionMethod != "pattern" && rule.DetectionMethod != "hybrid") errors.Add($"Supplier '{s.Name}' subtitle detection rule '{rule.Name}' has invalid detectionMethod '{rule.DetectionMethod}'");
                        }

                        if (sh.Assignments != null)
                        {
                            for (int a = 0; a < sh.Assignments.Count; a++)
                            {
                                var m = sh.Assignments[a];
                                if (m == null) { errors.Add($"Supplier '{s.Name}' subtitle assignment at index {a} is null"); continue; }
                                if (string.IsNullOrWhiteSpace(m.SourceKey)) errors.Add($"Supplier '{s.Name}' subtitle assignment[{a}] missing SourceKey");
                                if (string.IsNullOrWhiteSpace(m.TargetProperty)) errors.Add($"Supplier '{s.Name}' subtitle assignment[{a}] missing TargetProperty");
                                if (!string.IsNullOrWhiteSpace(m.LookupTable))
                                {
                                    var available = s.ParserConfig?.Lookups ?? new Dictionary<string, Dictionary<string,string>>();
                                    if (!available.ContainsKey(m.LookupTable)) errors.Add($"Supplier '{s.Name}' subtitle assignment[{a}] references unknown lookup table '{m.LookupTable}'");
                                }
                            }
                        }

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

                    if (s.ParserConfig == null)
                    {
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
                                if (string.IsNullOrEmpty(columnName)) errors.Add($"Supplier '{s.Name}' parserConfig contains a column with empty 'column' field at index {index}");
                                if (ruleCfg== null) errors.Add($"Supplier '{s.Name}' parserConfig column '{columnName}' missing rule");
                                else if (ruleCfg.Actions == null || ruleCfg.Actions.Count == 0) errors.Add($"Supplier '{s.Name}' parserConfig column '{columnName}' rule contains no actions");
                                else
                                {
                                    for (int aidx = 0; aidx < ruleCfg.Actions.Count; aidx++)
                                    {
                                        var act = ruleCfg.Actions[aidx];
                                        if (act == null) { errors.Add($"Supplier '{s.Name}' column '{columnName}' has null action at index {aidx}"); continue; }
                                        if (string.IsNullOrWhiteSpace(act.Op)) errors.Add($"Supplier '{s.Name}' column '{columnName}' action at index {aidx} missing Op");
                                        if (string.IsNullOrWhiteSpace(act.Input)) errors.Add($"Supplier '{s.Name}' column '{columnName}' action '{act.Op}' at index {aidx} missing Input");
                                        if (string.IsNullOrWhiteSpace(act.Output) && !string.Equals(act.Op, "noop", StringComparison.OrdinalIgnoreCase)) errors.Add($"Supplier '{s.Name}' column '{columnName}' action '{act.Op}' at index {aidx} missing Output");
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
            void AddErr(string msg) => errors.Add($"Supplier '{supplier.Name}' column '{columnName}' action[{actionIndex}] op='{op}': {msg}");

            switch (op.ToLowerInvariant())
            {
                case "assign":
                    if (parameters.Count > 0) AddErr($"unused Parameters: {string.Join(',', parameters.Keys)} (assign expects no parameters)");
                    break;
                case "find":
                    parameters.TryGetValue("Pattern", out var pattern);
                    parameters.TryGetValue("PatternKey", out var patternKey);

                    if (string.IsNullOrWhiteSpace(pattern) && string.IsNullOrWhiteSpace(patternKey))
                    {
                        AddErr("missing Parameters['Pattern'] or PatternKey");
                    }
                    else if (!string.IsNullOrWhiteSpace(pattern))
                    {
                        if (pattern.StartsWith("Lookup:", StringComparison.OrdinalIgnoreCase))
                        {
                            var tbl = pattern[("Lookup:").Length..].Trim();
                            var available = supplier.ParserConfig?.Lookups ?? new Dictionary<string, Dictionary<string,string>>();
                            if (!available.ContainsKey(tbl)) AddErr($"Pattern references lookup table '{tbl}' which is not present in merged lookups");
                        }
                        else
                        {
                            try { _ = new Regex(pattern); } catch (Exception ex) { AddErr($"invalid regex Pattern: {ex.Message}"); }
                        }
                    }
                    var allowedFind = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Pattern", "Options", "PatternKey" };
                    var unusedFind = parameters.Keys.Where(k => !allowedFind.Contains(k)).ToList();
                    if (unusedFind.Count > 0) AddErr($"unused Parameters: {string.Join(',', unusedFind)}");
                    break;
                case "split":
                case "splitbydelimiter":
                    var allowedSplit = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Delimiter", "ExpectedParts", "Strict" };
                    var unusedSplit = parameters.Keys.Where(k => !allowedSplit.Contains(k)).ToList();
                    if (unusedSplit.Count > 0) AddErr($"unused Parameters: {string.Join(',', unusedSplit)}");
                    break;
                case "map":
                case "mapping":
                    if (!parameters.TryGetValue("Table", out var tableName) || string.IsNullOrWhiteSpace(tableName))
                    {
                        AddErr("missing Parameters['Table'] for mapping action");
                    }
                    else
                    {
                        var available = supplier.ParserConfig?.Lookups ?? new Dictionary<string, Dictionary<string,string>>();
                        if (!available.ContainsKey(tableName)) AddErr($"mapping Parameters['Table'] references unknown lookup table '{tableName}'");
                    }
                    var allowedMap = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Table", "CaseMode", "AddIfNotFound" };
                    var unusedMap = parameters.Keys.Where(k => !allowedMap.Contains(k)).ToList();
                    if (unusedMap.Count > 0) AddErr($"unused Parameters: {string.Join(',', unusedMap)}");
                    break;
                case "convert":
                    // Validate convert with code-side constants (Preset-driven). Allow optional FromUnit/ToUnit overrides
                    var allowedConv = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Preset", "FromUnit", "ToUnit", "UnitKey", "SetUnit" };
                    var unusedConv = parameters.Keys.Where(k => !allowedConv.Contains(k)).ToList();
                    if (unusedConv.Count > 0) AddErr($"unused Parameters: {string.Join(',', unusedConv)}");
                    if (!parameters.ContainsKey("Preset") && !parameters.ContainsKey("FromUnit"))
                    {
                        AddErr("convert requires either Preset or FromUnit/ToUnit pair");
                    }
                    break;
                case "switch":
                case "case":
                    // Parameters: Default (optional), IgnoreCase (optional), When:<key>=<value> (multiple)
                    var allowedSwitch = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Default", "IgnoreCase" };
                    var whenParams = parameters.Keys.Where(k => k.StartsWith("When:", StringComparison.OrdinalIgnoreCase)).ToList();
                    var unusedSwitch = parameters.Keys.Where(k => !allowedSwitch.Contains(k) && !k.StartsWith("When:", StringComparison.OrdinalIgnoreCase)).ToList();
                    if (unusedSwitch.Count > 0) AddErr($"unused Parameters: {string.Join(',', unusedSwitch)}");
                    if (whenParams.Count == 0 && !parameters.ContainsKey("Default"))
                    {
                        AddErr("switch requires at least one When:<key>=<value> parameter or a Default value");
                    }
                    break;
                case "caseformat":
                    // Parameters: Mode (title|upper|lower), Culture (optional)
                    var allowedCase = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Mode", "Culture" };
                    var unusedCase = parameters.Keys.Where(k => !allowedCase.Contains(k)).ToList();
                    if (unusedCase.Count > 0) AddErr($"unused Parameters: {string.Join(',', unusedCase)}");
                    if (parameters.TryGetValue("Mode", out var mode))
                    {
                        var validModes = new[] { "title", "upper", "lower" };
                        if (!validModes.Contains(mode.ToLowerInvariant()))
                        {
                            AddErr($"invalid Mode '{mode}' (allowed: title, upper, lower)");
                        }
                    }
                    break;
                case "concat":
                    var allowedConcat = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Keys", "Separator" };
                    var unusedConcat = parameters.Keys.Where(k => !allowedConcat.Contains(k)).ToList();
                    if (unusedConcat.Count > 0) AddErr($"unused Parameters: {string.Join(',', unusedConcat)}");
                    if (!parameters.TryGetValue("Keys", out var keys) || string.IsNullOrWhiteSpace(keys))
                    {
                        AddErr("concat requires non-empty Parameters['Keys']");
                    }
                    break;
                case "clear":
                    if (parameters.Count > 0) AddErr($"unused Parameters: {string.Join(',', parameters.Keys)} (clear expects no parameters)");
                    break;
                default:
                    if (parameters.Count > 0) AddErr($"unknown Op '{op}' - cannot validate parameters but found: {string.Join(',', parameters.Keys)}");
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

        internal void UpdateFrom(SupplierConfiguration src, Dictionary<string, Dictionary<string, string>> mergedLookups)
        {
            if (src == null) throw new ArgumentNullException(nameof(src));
            // keep name (identity)
            this.Currency = src.Currency;

            // File structure
            if (this.FileStructure == null) this.FileStructure = new FileStructureConfiguration();
            this.FileStructure.ApplyFrom(src.FileStructure);

            // Subtitle handling
            if (src.SubtitleHandling == null)
            {
                this.SubtitleHandling = null;
            }
            else
            {
                this.SubtitleHandling ??= new SubtitleRowHandlingConfiguration();
                this.SubtitleHandling.ApplyFrom(src.SubtitleHandling);
            }

            // Parser config
            if (src.ParserConfig == null)
            {
                this.ParserConfig = null;
            }
            else
            {
                this.ParserConfig ??= new ParserConfig();
                this.ParserConfig.ApplyFrom(src.ParserConfig);
                // re-merge lookups
                this.ParserConfig.DoMergeLoookUpTables(mergedLookups);
            }
        }
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

    internal static class FileStructureConfigurationExtensions
    {
        public static void ApplyFrom(this FileStructureConfiguration dst, FileStructureConfiguration src)
        {
            if (dst == null) throw new ArgumentNullException(nameof(dst));
            if (src == null) return;
            dst.DataStartRowIndex = src.DataStartRowIndex;
            dst.ExpectedColumnCount = src.ExpectedColumnCount;
            dst.HeaderRowIndex = src.HeaderRowIndex;

            if (src.Detection == null)
            {
                dst.Detection = null;
            }
            else
            {
                dst.Detection ??= new DetectionConfiguration();
                dst.Detection.ApplyFrom(src.Detection);
            }
        }
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
        public List<SubtitleAssignmentMapping> Assignments { get; set; } = new();
        public List<SubtitleTransformRule> Transforms { get; set; } = new();
    }

    internal static class SubtitleRowHandlingConfigurationExtensions
    {
        public static void ApplyFrom(this SubtitleRowHandlingConfiguration dst, SubtitleRowHandlingConfiguration src)
        {
            if (dst == null) throw new ArgumentNullException(nameof(dst));
            if (src == null) return;
            dst.Enabled = src.Enabled;
            dst.Action = src.Action;
            dst.FallbackAction = src.FallbackAction;

            // Replace lists content
            dst.DetectionRules.Clear();
            if (src.DetectionRules != null)
                dst.DetectionRules.AddRange(src.DetectionRules.Select(r => new SubtitleDetectionRule
                {
                    Name = r.Name,
                    Description = r.Description,
                    DetectionMethod = r.DetectionMethod,
                    ExpectedColumnCount = r.ExpectedColumnCount,
                    ApplyToSubsequentRows = r.ApplyToSubsequentRows,
                    ValidationPatterns = new List<string>(r.ValidationPatterns ?? new())
                }));

            dst.Assignments.Clear();
            if (src.Assignments != null)
                dst.Assignments.AddRange(src.Assignments.Select(a => new SubtitleAssignmentMapping
                {
                    SourceKey = a.SourceKey,
                    TargetProperty = a.TargetProperty,
                    LookupTable = a.LookupTable,
                    Overwrite = a.Overwrite
                }));

            dst.Transforms.Clear();
            if (src.Transforms != null)
                dst.Transforms.AddRange(src.Transforms.Select(t => new SubtitleTransformRule
                {
                    SourceKey = t.SourceKey,
                    Mode = t.Mode,
                    Pattern = t.Pattern,
                    Replacement = t.Replacement,
                    IgnoreCase = t.IgnoreCase
                }));
        }
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

    internal static class DetectionConfigurationExtensions
    {
        public static void ApplyFrom(this DetectionConfiguration dst, DetectionConfiguration src)
        {
            if (dst == null) throw new ArgumentNullException(nameof(dst));
            if (src == null) return;
            dst.FileNamePatterns.Clear();
            if (src.FileNamePatterns != null)
                dst.FileNamePatterns.AddRange(src.FileNamePatterns);
        }
    }
}

namespace ParsingEngine
{
    internal static class ParserConfigExtensions
    {
        public static void ApplyFrom(this ParserConfig dst, ParserConfig src)
        {
            if (dst == null) throw new ArgumentNullException(nameof(dst));
            if (src == null) return;

            // Settings
            if (dst.Settings == null) dst.Settings = new Settings();
            if (src.Settings != null)
            {
                dst.Settings.StopOnFirstMatchPerColumn = src.Settings.StopOnFirstMatchPerColumn;
                dst.Settings.DefaultCulture = src.Settings.DefaultCulture;
                dst.Settings.PreferFirstAssignment = src.Settings.PreferFirstAssignment;
            }

            // Lookups: replace content in-place
            dst.Lookups ??= new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            InPlaceMerge(dst.Lookups, src.Lookups ?? new(StringComparer.OrdinalIgnoreCase));

            // ColumnRules: update in-place to keep RuleConfig instance identities where possible
            dst.ColumnRules ??= new Dictionary<string, RuleConfig>(StringComparer.OrdinalIgnoreCase);
            // Remove missing
            var toRemove = dst.ColumnRules.Keys.Where(k => !src.ColumnRules.ContainsKey(k)).ToList();
            foreach (var k in toRemove) dst.ColumnRules.Remove(k);
            // Upsert
            foreach (var kv in src.ColumnRules)
            {
                if (!dst.ColumnRules.TryGetValue(kv.Key, out var existing))
                {
                    dst.ColumnRules[kv.Key] = CloneRuleConfig(kv.Value);
                }
                else
                {
                    ApplyToRuleConfig(existing, kv.Value);
                }
            }
        }

        private static void InPlaceMerge(Dictionary<string, Dictionary<string, string>> target, Dictionary<string, Dictionary<string, string>> source)
        {
            if (!(target.Comparer is StringComparer sc && sc.Equals(StringComparer.OrdinalIgnoreCase)))
            {
                var copy = new Dictionary<string, Dictionary<string, string>>(target, StringComparer.OrdinalIgnoreCase);
                target.Clear();
                foreach (var kv in copy) target[kv.Key] = kv.Value;
            }

            var remove = target.Keys.Where(k => !source.ContainsKey(k)).ToList();
            foreach (var k in remove) target.Remove(k);

            foreach (var tbl in source)
            {
                if (!target.TryGetValue(tbl.Key, out var inner))
                {
                    target[tbl.Key] = new Dictionary<string, string>(tbl.Value ?? new(), StringComparer.OrdinalIgnoreCase);
                }
                else
                {
                    if (!(inner.Comparer is StringComparer sci && sci.Equals(StringComparer.OrdinalIgnoreCase)))
                    {
                        inner = new Dictionary<string, string>(inner, StringComparer.OrdinalIgnoreCase);
                        target[tbl.Key] = inner;
                    }
                    var remInner = inner.Keys.Where(k => !(tbl.Value ?? new()).ContainsKey(k)).ToList();
                    foreach (var k in remInner) inner.Remove(k);
                    foreach (var kv in (tbl.Value ?? new()))
                    {
                        if (kv.Key == null) continue;
                        inner[kv.Key] = kv.Value;
                    }
                }
            }
        }

        private static RuleConfig CloneRuleConfig(RuleConfig src)
        {
            var rc = new RuleConfig
            {
                Trace = src.Trace,
                Actions = src.Actions?.Select(CloneAction).ToList() ?? new()
            };
            return rc;
        }

        private static void ApplyToRuleConfig(RuleConfig dst, RuleConfig src)
        {
            dst.Trace = src.Trace;
            dst.Actions = src.Actions?.Select(CloneAction).ToList() ?? new();
        }

        private static ActionConfig CloneAction(ActionConfig a)
        {
            return new ActionConfig
            {
                Op = a.Op,
                Input = a.Input,
                Output = a.Output,
                Assign = a.Assign,
                Condition = a.Condition,
                Parameters = a.Parameters == null ? null : new Dictionary<string, string>(a.Parameters, StringComparer.OrdinalIgnoreCase)
            };
        }
    }
}