namespace ParsingEngine;

/// <summary>
/// MappingAction: looks up an input value in a named lookup table and writes the mapped value to output.
/// Parameters (via ActionConfig.Parameters):
/// - table: lookup table name (required)
/// - casemode: "exact" (default), "upper", "lower"
/// - assign: "true" to write to assign:{Output} instead of Output
/// </summary>
public sealed class MappingAction : IChainAction
{
    public string Op => "map";
    private readonly string _fromKey;
    private readonly string _toKey;
    private readonly bool _assign;
    private readonly Dictionary<string, string> _lookup;

    public MappingAction(string fromKey, string toKey, bool assign, Dictionary<string, string> lookup)
    {
        if (string.IsNullOrWhiteSpace(fromKey)) throw new ArgumentException("fromKey required", nameof(fromKey));
        if (string.IsNullOrWhiteSpace(toKey)) throw new ArgumentException("toKey required", nameof(toKey));
        ArgumentNullException.ThrowIfNull(lookup);

        _fromKey = fromKey;
        _toKey = toKey;
        _assign = assign;
        _lookup = lookup;
    }

    public bool Execute(IDictionary<string, string> bag, CellContext ctx)
    {
        if (bag == null) throw new ArgumentNullException(nameof(bag));

        bag.TryGetValue(_fromKey, out var raw);
        var input = raw ?? string.Empty;
        if (string.IsNullOrEmpty(input)) return false;


        foreach (var kv in _lookup)
        {
            if (string.Equals(kv.Key, input, StringComparison.OrdinalIgnoreCase))
            {
                if (_assign)
                    bag[$"assign:{_toKey}"] = kv.Value;
                else
                    bag[_toKey] = kv.Value;
                return true;
            }
        }

        // not found
        return false;
    }
}
