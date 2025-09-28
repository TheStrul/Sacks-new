namespace ParsingEngine;

/// <summary>
/// MappingAction: looks up an input value in a named lookup table and writes the mapped value to output.
/// Parameters (via ActionConfig.Parameters):
/// - table: lookup table name (required)
/// - casemode: "exact" (default), "upper", "lower"
/// - assign: "true" to write to assign:{Output} instead of Output
/// </summary>
public sealed class MappingAction : BaseAction
{
    public override string Op => "map";
    private readonly Dictionary<string, string> _lookup;

    public MappingAction(string fromKey, string toKey, bool assign, Dictionary<string, string> lookup, string? condition = null) : base(fromKey, toKey,assign,condition)
    {
        _lookup = lookup;
    }

    public override bool Execute(IDictionary<string, string> bag, CellContext ctx)
    {
        if (base.Execute(bag, ctx) == false) return false; // basic checks

        bag.TryGetValue(base.input, out var raw);
        var input = raw ?? string.Empty;
        if (string.IsNullOrEmpty(input)) return false;


        foreach (var kv in _lookup)
        {
            if (string.Equals(kv.Key, input, StringComparison.OrdinalIgnoreCase))
            {
                if (base.assign)
                    bag[$"assign:{base.output}"] = kv.Value;
                else
                    bag[base.output] = kv.Value;
                return true;
            }
        }

        // not found
        return false;
    }
}
