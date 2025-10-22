namespace ParsingEngine;

/// <summary>
/// Concatenate multiple bag keys into a single output string with a separator.
/// Reads both plain keys and their assigned variants (assign:Key).
/// Parameters:
/// - Keys: comma-separated list of input keys to concatenate (required)
/// - Separator: string placed between non-empty parts (default: single space)
/// </summary>
public sealed class ConcatAction : BaseAction
{
    public override string Op => "concat";
    private readonly string[] _keys;
    private readonly string _sep;

    public ConcatAction(string fromKey, string toKey, bool assign, string? condition, IEnumerable<string> keys, string? separator)
        : base(fromKey, toKey, assign, condition)
    {
        _keys = keys?.Select(k => (k ?? string.Empty).Trim()).Where(k => k.Length > 0).ToArray() ?? Array.Empty<string>();
        _sep = separator ?? " ";
    }

    public override bool Execute(CellContext ctx)
    {
        if (!base.Execute(ctx)) return false;
        if (_keys.Length == 0) return false;

        var parts = new List<string>();
        foreach (var k in _keys)
        {
            if (ctx.PropertyBag.Variables.TryGetValue(k, out var v) && !string.IsNullOrWhiteSpace(v))
            {
                parts.Add(v.Trim());
                continue;
            }
            // also check assigned form
            if (ctx.PropertyBag.Assignes.TryGetValue(k, out var va) && va != null)
            {
                parts.Add(va.ToString()!.Trim());
            }
        }

        var joined = string.Join(_sep, parts.Where(p => !string.IsNullOrWhiteSpace(p)));
        if (string.IsNullOrWhiteSpace(joined)) return false;

        if (assign)
        {
            ctx.PropertyBag.SetAssign(output, joined);
        }
        else
        {
            ctx.PropertyBag.SetVariable(output, joined);
        }
        return true;
    }
}
