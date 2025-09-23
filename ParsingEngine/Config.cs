using System.Text.Json.Serialization;

namespace ParsingEngine;

public sealed class ParserConfig
{
    public Settings Settings { get; set; } = new();
    public Dictionary<string, Dictionary<string, string>> Lookups { get; set; } = new();
    public List<ColumnConfig> Columns { get; set; } = new();

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

public sealed class ColumnConfig
{
    public string Column { get; set; } = "";
    // Single rule per column
    public RuleConfig? Rule { get; set; }
}



public class ActionConfig
{
    [JsonPropertyName("op")]
    public string Op { get; set; } = string.Empty;

    // Accept both legacy "in"/"input" and modern "Input" names via alias properties
    [JsonIgnore]
    public string Input { get; set; } = "Text";
    [JsonPropertyName("in")]
    public string? In { set => Input = value ?? Input; }
    [JsonPropertyName("input")]
    public string? InputAlias { set => Input = value ?? Input; }

    // Accept both legacy "out"/"output" and modern "Output" names via alias properties
    [JsonIgnore]
    public string Output { get; set; } = string.Empty;
    [JsonPropertyName("out")]
    public string? Out { set => Output = value ?? Output; }
    [JsonPropertyName("output")]
    public string? OutputAlias { set => Output = value ?? Output; }

    // Parameters dictionary (matches JSON "Parameters" key)
    public Dictionary<string,string>? Parameters { get; set; }
}

public sealed class RuleConfig
{
    public Dictionary<string, string>? Assign { get; set; }

    public List<ActionConfig>? Actions { get; set; }

    // When true, emit per-step trace lines (input/output) during execution
    public bool Trace { get; set; } = true;
}


// Base step abstraction for pipeline operations
public abstract class BaseStepConfig
{
    [JsonPropertyName("op")]
    public string Op { get; set; } = "";

    // Unified IO contract for all steps. Provide alias properties to support legacy JSON fields.
    [JsonIgnore]
    public string Input { get; set; } = "Text";

    [JsonPropertyName("in")]
    public string? In { set => Input = value ?? Input; }
    [JsonPropertyName("input")]
    public string? InputAlias { set => Input = value ?? Input; }
    [JsonPropertyName("from")]
    public string? From { set => Input = value ?? Input; }

    [JsonIgnore]
    public string Output { get; set; } = string.Empty;

    [JsonPropertyName("out")]
    public string? Out { set => Output = value ?? Output; }
    [JsonPropertyName("output")]
    public string? OutputAlias { set => Output = value ?? Output; }
    [JsonPropertyName("to")]
    public string? To { set => Output = value ?? Output; }
}




