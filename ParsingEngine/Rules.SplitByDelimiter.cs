namespace ParsingEngine;

public sealed class SplitByDelimiterRule : IRule
{
    public string Id { get; }
    public int Priority { get; }
    private readonly string _delimiter;
    private readonly List<SplitMapping> _maps;

    public SplitByDelimiterRule(RuleConfig rc)
    {
        ArgumentNullException.ThrowIfNull(rc);
        Id = rc.Id; Priority = rc.Priority;
        _delimiter = rc.Delimiter ?? ".";
        _maps = rc.Mappings ?? new();
    }

    public RuleExecutionResult Execute(CellContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        if (string.IsNullOrWhiteSpace(ctx.Raw)) return new(false, new());
        var parts = ctx.Raw.Split(_delimiter, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var list = new List<Assignment>();
        foreach (var p in parts)
        {
            foreach (var m in _maps)
            {
                if (p.TrimStart().StartsWith(m.StartsWith, StringComparison.OrdinalIgnoreCase))
                {
                    var val = p.Trim().Substring(m.After.Length).Trim();
                    list.Add(new Assignment(m.AssignTo, val, Id));
                }
            }
        }
        return new(list.Count > 0, list);
    }
}
