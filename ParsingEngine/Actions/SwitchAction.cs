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

    public override bool Execute(CellContext ctx)
    {
        if (base.Execute(ctx) == false) return false;

        ctx.PropertyBag.Variables.TryGetValue(input, out var src);
        var key = src ?? string.Empty;

        if (_cases.TryGetValue(key, out var mapped))
        {
            if (assign)
                ctx.PropertyBag.SetAssign(output, mapped ?? string.Empty);
            else
                ctx.PropertyBag.SetVariable(output, mapped ?? string.Empty);
            return true;
        }

        if (_default != null)
        {
            if (assign)
                ctx.PropertyBag.SetAssign(output, _default);
            else
                ctx.PropertyBag.SetVariable(output, _default);
            return true;
        }

        return false;
    }
}
