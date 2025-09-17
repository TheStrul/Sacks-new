namespace ParsingEngine;

public sealed class DirectAssignRule : IRule
{
    public string Id { get; }
    public int Priority { get; }
    private readonly Dictionary<string, string> _assign;

    public DirectAssignRule(RuleConfig rc)
    {
        ArgumentNullException.ThrowIfNull(rc);
        Id = rc.Id; Priority = rc.Priority;
        _assign = rc.Assign ?? new();
    }

    public RuleExecutionResult Execute(CellContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        if (string.IsNullOrWhiteSpace(ctx.Raw)) return new(false, new());
        var list = new List<Assignment>();
        foreach (var kvp in _assign)
        {
            var property = kvp.Key;
            object? value = ConvertValue(ctx.Raw!, kvp.Value);
            list.Add(new Assignment(property, value, Id));
        }
        return new(true, list);
    }

    private static object? ConvertValue(string raw, string? converter) =>
        converter switch { "trim" => raw.Trim(), "decimal" => decimal.TryParse(raw.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : (object?)raw, _ => raw };
}
