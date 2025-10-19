namespace ParsingEngine;

public sealed class SwitchAction : BaseAction
{
    public override string Op => "switch";

    private readonly Dictionary<string, string> _cases;
    private readonly string? _default;
    private readonly StringComparer _cmp;

    public SwitchAction(
        string fromKey,
        string toKey,
        bool assign,
        string? condition,
        Dictionary<string, string> cases,
        string? @default,
        bool ignoreCase)
        : base(fromKey, toKey, assign, condition)
    {
        _cmp = ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        _cases = new Dictionary<string, string>(cases ?? new(), _cmp);
        _default = @default;
    }

    public override bool Execute(IDictionary<string, string> bag, CellContext ctx)
    {
        if (base.Execute(bag, ctx) == false) return false;

        bag.TryGetValue(input, out var src);
        var key = src ?? string.Empty;

        if (_cases.TryGetValue(key, out var mapped))
        {
            if (assign)
                bag[$"assign:{output}"] = mapped ?? string.Empty;
            else
                bag[output] = mapped ?? string.Empty;
            return true;
        }

        if (_default != null)
        {
            if (assign)
                bag[$"assign:{output}"] = _default;
            else
                bag[output] = _default;
            return true;
        }

        return false;
    }
}
