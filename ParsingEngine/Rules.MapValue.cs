namespace ParsingEngine;

public sealed class MapValueRule : IRule
{
    public string Id { get; }
    public int Priority { get; }
    private readonly Dictionary<string,string> _assign;

    public MapValueRule(RuleConfig rc)
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
            var parts = kvp.Key.Split("->");
            var property = parts[1];
            object? value = ConvertValue(ctx.Raw!, kvp.Value);
            list.Add(new Assignment(property, value, Id));
        }
        return new(true, list);
    }

    private static object? ConvertValue(string raw, string converter) =>
        converter switch { "trim" => raw.Trim(), _ => raw };
}
