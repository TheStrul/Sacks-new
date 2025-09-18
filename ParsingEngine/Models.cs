using System.Globalization;

namespace ParsingEngine;

public record RowData(Dictionary<string, string> Cells);
public record Assignment(string Property, object? Value, string Source);
public record RuleExecutionResult(bool Matched, List<Assignment> Assignments);
public record CellContext(string Column, string? Raw, CultureInfo Culture, IDictionary<string, object?> Ambient);

public sealed class PropertyBag
{
    public Dictionary<string, object?> Values { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> Trace { get; } = new();
    public bool PreferFirstAssignment { get; init; } = false;

    public void Set(string prop, object? value, string source)
    {
        if (PreferFirstAssignment && Values.ContainsKey(prop))
        {
            Trace.Add($"{prop}='{Values[prop]}' (kept) ← {source} (skipped overwrite)");
            return;
        }
        Values[prop] = value;
        Trace.Add($"{prop}='{value}' ← {source}");
    }
}

