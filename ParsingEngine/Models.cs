using System.Globalization;

namespace ParsingEngine;

public record RowData(Dictionary<string, string> Cells);
public record Assignment(string Property, object? Value);
public record CellContext(string Column, string? Raw, CultureInfo Culture, PropertyBag PropertyBag);

public sealed class PropertyBag
{
    public Dictionary<string, object?> Assignes { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> Variables { get; } = new(StringComparer.OrdinalIgnoreCase);

    public void SetAssign(string prop, object? value)
    {
        Assignes[prop] = value;
    }

    public void SetVariable(string variable, string value)
    {
        Variables[variable] = value;
    }
}

