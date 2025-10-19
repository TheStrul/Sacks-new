using System.Globalization;
using System.Text.RegularExpressions;

namespace ParsingEngine;

/// <summary>
/// Compatibility factory that creates parsing steps or chain actions from config objects.
/// </summary>
public static class ActionsFactory
{


    // create chain action from ActionConfig
    public static IChainAction Create(ActionConfig s, Dictionary<string, Dictionary<string, string>> lookups)
    {
        if (s == null) throw new ArgumentNullException(nameof(s));
        if (lookups == null) throw new ArgumentNullException(nameof(lookups));

        var op = (s.Op ?? string.Empty).ToLowerInvariant();
        var input = s.Input ?? string.Empty;
        var output = s.Output ?? string.Empty;
        var patameter = s.Parameters ?? new Dictionary<string,string>();
        var assign = s.Assign;
        var condition = s.Condition;
        IChainAction? ret;
        switch (op)
        {
            case "assign":
                ret = new SimpleAssignAction(input, output,assign,condition);
                break;
            case "find":
                string pattern = patameter.ContainsKey("Pattern") ? patameter["Pattern"] : string.Empty;
                string? patternKey = patameter.ContainsKey("PatternKey") ? patameter["PatternKey"] : null;

                // Support special pattern: lookup:<tableName> - pass lookup table entries (preserve order)
                List<KeyValuePair<string,string>>? lookupEntries = null;
                if (!string.IsNullOrEmpty(pattern) && pattern.StartsWith("lookup:", StringComparison.OrdinalIgnoreCase))
                {
                    var tblName = pattern[("lookup:").Length..].Trim();
                    if (!string.IsNullOrEmpty(tblName) && lookups.TryGetValue(tblName, out var table))
                    {
                        // Preserve the dictionary enumeration order by materializing a list of entries
                        lookupEntries = table.ToList();
                        pattern = string.Empty; // FindAction will use lookupEntries when provided
                    }
                }

                string[] options = patameter.ContainsKey("Options") ? patameter["Options"].Split(new[]{','}, StringSplitOptions.RemoveEmptyEntries  |StringSplitOptions.TrimEntries) : Array.Empty<string>();
                ret = new FindAction(input, output, pattern, options.ToList(), assign, condition, lookupEntries, patternKey);
                break;
            case "split":
                // delimiter parameter expected in Parameters["delimiter"], default to ':'
                var delimiter = patameter.ContainsKey("Delimiter") ? patameter["Delimiter"] : ":";
                ret = new SplitAction(input, output, delimiter);
                break;
            case "map":
            case "mapping":
                string tableName = patameter["Table"];
                // Resolve lookup table
                var mapDict = lookups[tableName];
                var addIfNotFound = false;
                if (patameter.TryGetValue("AddIfNotFound", out var addStr))
                {
                    addIfNotFound = string.Equals(addStr, "true", StringComparison.OrdinalIgnoreCase);
                }
                ret = new MappingAction(input, output, assign, mapDict, condition, addIfNotFound);
                break;
            case "convert":
                // Parameters: Preset, FromUnit, ToUnit, UnitKey, SetUnit
                patameter.TryGetValue("FromUnit", out var fromUnit);
                patameter.TryGetValue("ToUnit", out var toUnit);
                patameter.TryGetValue("UnitKey", out var unitKey);
                var setUnit = true;
                if (patameter.TryGetValue("SetUnit", out var setUnitStr))
                {
                    setUnit = string.Equals(setUnitStr, "true", StringComparison.OrdinalIgnoreCase);
                }

                // Preset-based constants (kept in code, not JSON)
                patameter.TryGetValue("Preset", out var preset);
                double factor;
                string? roundMode;
                if (!string.IsNullOrWhiteSpace(preset) && preset.Equals("OzToMl", StringComparison.OrdinalIgnoreCase))
                {
                    // 1 oz = 29.5735 ml; snap handled in action
                    factor = 29.5735d;
                    roundMode = null; // snapping overrides rounding
                    fromUnit ??= "oz";
                    toUnit ??= "ml";
                }
                else
                {
                    // Fallback defaults if no preset: identity/no rounding
                    factor = 1d;
                    roundMode = null;
                }
                ret = new ConvertAction(input, output, assign, condition, fromUnit, toUnit, factor, unitKey, roundMode, setUnit);
                break;
            case "case":
            case "switch":
                // Parameters: Default, IgnoreCase, and any When:<text>=<value>
                var ignoreCase = false;
                if (patameter.TryGetValue("IgnoreCase", out var icStr))
                    ignoreCase = string.Equals(icStr, "true", StringComparison.OrdinalIgnoreCase);
                patameter.TryGetValue("Default", out var defVal);
                var cases = new Dictionary<string, string>(ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
                foreach (var kv in patameter)
                {
                    if (kv.Key.StartsWith("When:", StringComparison.OrdinalIgnoreCase))
                    {
                        var k = kv.Key[5..];
                        cases[k] = kv.Value;
                    }
                }
                ret = new SwitchAction(input, output, assign, condition, cases, defVal, ignoreCase);
                break;
            case "caseformat":
                var mode = patameter.ContainsKey("Mode") ? patameter["Mode"] : "title";
                var culture = patameter.ContainsKey("Culture") ? patameter["Culture"] : null;
                ret = new CaseAction(input, output, assign, condition, mode, culture);
                break;
            case "concat":
                var keys = patameter.ContainsKey("Keys") ? patameter["Keys"].Split(new[]{','}, StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries) : Array.Empty<string>();
                patameter.TryGetValue("Separator", out var sep);
                ret = new ConcatAction(input, output, assign, condition, keys, sep);
                break;
            case "clear":
                ret = new ClearAction(input, output, assign, condition);
                break;
            default:
                ret = new NoOpChainAction(input, output);
                break;
        }

        return ret!;
    }

    private sealed class NoOpChainAction : IChainAction
    {
        private readonly string _from;
        private readonly string _to;
        public NoOpChainAction(string from, string to)
        {
            _from = from;
            _to = to;
        }
        public string Op => "noop";
        public bool Execute(IDictionary<string, string> bag, CellContext ctx)
        {
            if (bag == null) throw new ArgumentNullException(nameof(bag));
            bag.TryGetValue(_from, out var value);
            bag[_to] = value ?? string.Empty;
            return true;
        }
    }
}
