using System.Text.Json;

namespace ParsingEngine;

public sealed class ParserConfig
{
    public Settings Settings { get; set; } = new();
    public Dictionary<string, Dictionary<string, string>> Lookups { get; set; } = new();
    public List<ColumnConfig> Columns { get; set; } = new();
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

public sealed class ExecutionConfig
{
    public Dictionary<string, string>? Assign { get; set; }
    public List<ActionConfig>? Steps { get; set; }
    // When true, emit per-step trace lines (input/output) during execution
    public bool Trace { get; set; } = true;
}

public class ActionConfig
{
    required public string Op { get; set; }
    required public string Input { get; set; }
    required public string Output { get; set; }

    // Parameters dictionary (matches JSON "Parameters" key)
    public Dictionary<string,string>? Parameters { get; set; }


}

public sealed class RuleConfig
{
    public Dictionary<string, string>? Assign { get; set; }
    public List<PipelineStepConfig>? Steps { get; set; }

    public List<ActionConfig>? Actions { get; set; }

    // When true, emit per-step trace lines (input/output) during execution
    public bool Trace { get; set; } = true;
}
public sealed class SplitMapping
{
    public string AssignTo { get; set; } = "";
    public string Table { get; set; } = ""; // Lookup table name for ConditionalMapping
}

// Base step abstraction for pipeline operations
public abstract class BaseStepConfig
{
    public string Op { get; set; } = "";
    // Unified IO contract for all steps. Prefer these over legacy From/To/In/Out.
    required public string Input { get; set; }
    required public string Output { get; set; }
}

// Marker type for parsing-oriented steps (no extra members yet)
public class ParsingStep : BaseStepConfig { }

// Marker type for transformation-oriented steps (no extra members yet)
public class TransformationStep : BaseStepConfig { }

public sealed class PipelineStepConfig : BaseStepConfig
{
    public string? Pattern { get; set; }
    public string? Options { get; set; }
    public string? Replacement { get; set; } // For RegexReplace operation - replacement text
    public string? Table { get; set; }
    public string? ValueOut { get; set; }
    public string? Form { get; set; }
    public string? CaseMode { get; set; } // "exact", "lower", "upper"
    public string? WordOut { get; set; } // For ExtractLastWord operation
    public string? RemainingOut { get; set; } // For operations that extract and leave remaining text
    public string? ExtractedOut { get; set; } // For generic extraction operations
    public string? SizeOut { get; set; } // For ExtractSize operation
    public string[]? Patterns { get; set; } // For operations that use multiple patterns
    public string? Delimiter { get; set; } // For SplitByDelimiter operation
    public string? OutputProperty { get; set; } // For SplitByDelimiter operation output property name
    public int? ExpectedParts { get; set; } // For SplitByDelimiter operation - expected number of parts
    public bool? Strict { get; set; } // For SplitByDelimiter operation - strict validation mode
    public List<SplitMapping>? Mappings { get; set; } // For ConditionalMapping operation - mapping rules
}

public static class ParserConfigLoader
{
    private static readonly JsonSerializerOptions s_jsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        // Allow legacy JSON fields (e.g., "type") without failing deserialization
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static ParserConfig FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var cfg = System.Text.Json.JsonSerializer.Deserialize<ParserConfig>(json, s_jsonOptions)
                  ?? throw new InvalidOperationException("Failed to parse rules JSON");
        return cfg;
    }

    public static ParserConfig FromJsonFile(string path)
    {
        var json = File.ReadAllText(path);
        var cfg = System.Text.Json.JsonSerializer.Deserialize<ParserConfig>(json, s_jsonOptions) ?? throw new InvalidOperationException("Failed to parse rules JSON");

        return cfg;
    }
}
