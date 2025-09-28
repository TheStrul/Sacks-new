using System.Text.Json.Serialization;

namespace ParsingEngine;

public sealed class ParserConfig
{
    public Settings Settings { get; set; } = new();
    public Dictionary<string, Dictionary<string, string>> Lookups { get; set; } = new();
    public Dictionary<string, RuleConfig> ColumnRules { get; set; } = new();

    public void DoMergeLoookUpTables(Dictionary<string, Dictionary<string, string>> lookups)
    {
        if (lookups == null) throw new ArgumentNullException(nameof(lookups));

        // Ensure top-level lookups dictionary is case-insensitive
        if (!(Lookups.Comparer is StringComparer scTop && scTop.Equals(StringComparer.OrdinalIgnoreCase)))
        {
            var copy = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in Lookups)
                copy[kv.Key] = kv.Value;
            Lookups = copy;
        }

        foreach (var tbl in lookups)
        {
            if (tbl.Key == null) continue;
            var tableName = tbl.Key.Trim();
            if (string.IsNullOrEmpty(tableName)) continue;

            var incoming = tbl.Value ?? new Dictionary<string, string>();

            if (!Lookups.TryGetValue(tableName, out var existing))
            {
                // create a case-insensitive inner dictionary and copy incoming values
                var newInner = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var kv in incoming)
                {
                    if (kv.Key == null) continue;
                    newInner[kv.Key] = kv.Value;
                }
                Lookups[tableName] = newInner;
            }
            else
            {
                // ensure existing inner dictionary uses case-insensitive comparer
                if (!(existing.Comparer is StringComparer scInner && scInner.Equals(StringComparer.OrdinalIgnoreCase)))
                {
                    var newInner = new Dictionary<string, string>(existing, StringComparer.OrdinalIgnoreCase);
                    existing = newInner;
                    Lookups[tableName] = existing;
                }

                // Merge: incoming entries overwrite existing values for same keys
                foreach (var kv in incoming)
                {
                    if (kv.Key == null) continue;
                    existing[kv.Key] = kv.Value;
                }
            }
        }
    }
}
public sealed class Settings
{
    public bool StopOnFirstMatchPerColumn { get; set; } = false;
    public string? DefaultCulture { get; set; } = "en-US";
    public bool PreferFirstAssignment { get; set; } = false;
}


public class ActionConfig
{
    public required string Op { get; set; }

    public required string Input { get; set; }

    public required string Output { get; set; }

    public bool Assign { get; set; } = false;

    public string? Condition { get; set; } = null;

    public Dictionary<string,string>? Parameters { get; set; }
}

public sealed class RuleConfig
{
    [JsonIgnore]
    public Dictionary<string, string> Assign { get;} = new Dictionary<string, string>();

    required public List<ActionConfig> Actions { get; set; }

    // When true, emit per-step trace lines (input/output) during execution
    public bool Trace { get; set; }
}
