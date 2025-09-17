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
    public List<RuleConfig> Rules { get; set; } = new();
}
public sealed class RuleConfig
{
    public string Id { get; set; } = "";
    public int Priority { get; set; } = 0;
    public string Type { get; set; } = ""; // MultiCaptureRegex | SplitByDelimiter | Pipeline | MapValue
    public string? Pattern { get; set; }
    public string? Delimiter { get; set; }
    public Dictionary<string, string>? Assign { get; set; }
    public List<SplitMapping>? Mappings { get; set; }
    public List<PipelineStep>? Steps { get; set; }
}
public sealed class SplitMapping
{
    public string StartsWith { get; set; } = "";
    public string AssignTo { get; set; } = "";
    public string After { get; set; } = "";
}
public sealed class PipelineStep
{
    public string Op { get; set; } = "";
    public string? Pattern { get; set; }
    public string? Options { get; set; }
    public string? Table { get; set; }
    public string? In { get; set; }
    public string? Out { get; set; }
    public string? From { get; set; }
    public string? To { get; set; }
    public string? ValueFrom { get; set; }
    public string? UnitFrom { get; set; }
    public string? UnitOut { get; set; }
    public string? ValueOut { get; set; }
    public string? Form { get; set; }
    public string? CaseMode { get; set; } // "exact", "lower", "upper"
    public string? WordOut { get; set; } // For ExtractLastWord operation
    public string? RemainingOut { get; set; } // For operations that extract and leave remaining text
    public string? ExtractedOut { get; set; } // For generic extraction operations
    public string? SizeOut { get; set; } // For ExtractSize operation
    public string[]? Patterns { get; set; } // For operations that use multiple patterns
    public Dictionary<string,string>? Map { get; set; }
}

public static class ParserConfigLoader
{
    private static readonly JsonSerializerOptions s_jsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    public static ParserConfig FromJsonFile(string path)
    {
        var json = File.ReadAllText(path);
        var cfg = System.Text.Json.JsonSerializer.Deserialize<ParserConfig>(json, s_jsonOptions) ?? throw new InvalidOperationException("Failed to parse rules JSON");

        return cfg;
    }
}
