namespace ParsingEngine;

/// <summary>
/// MappingAction: looks up an input value in a named lookup table and writes the mapped value to output.
/// Parameters (via ActionConfig.Parameters):
/// - table: lookup table name (required)
/// - casemode: "exact" (default), "upper", "lower"
/// - AddIfNotFound: "true" to add missing key (as lower-case) with value in Title Case
/// </summary>
public sealed class MappingAction : BaseAction
{
    public override string Op => "map";
    private readonly Dictionary<string, string> _lookup;
    private readonly bool _addIfNotFound;

    public MappingAction(string fromKey, string toKey, bool assign, Dictionary<string, string> lookup, string? condition = null, bool addIfNotFound = false)
        : base(fromKey, toKey, assign, condition)
    {
        _lookup = lookup ?? throw new ArgumentNullException(nameof(lookup));
        _addIfNotFound = addIfNotFound;
    }

    public override bool Execute(IDictionary<string, string> bag, CellContext ctx)
    {
        if (base.Execute(bag, ctx) == false) return false; // basic checks

        bag.TryGetValue(base.input, out var raw);
        var input = raw ?? string.Empty;
        if (string.IsNullOrWhiteSpace(input)) return false;

        // Try to find in lookup (case-insensitive)
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

        // Not found - optionally add
        if (_addIfNotFound)
        {
            // Key: lower-case trimmed, Value: Title Case based on current context culture
            var key = (input ?? string.Empty).Trim().ToLowerInvariant();
            var culture = ctx.Culture ?? System.Globalization.CultureInfo.InvariantCulture;
            var titleValue = culture.TextInfo.ToTitleCase((input ?? string.Empty).Trim().ToLower(culture));

            // Add or overwrite existing (dictionary is case-insensitive per config merging)
            _lookup[key] = titleValue;

            if (base.assign)
                bag[$"assign:{base.output}"] = titleValue;
            else
                bag[base.output] = titleValue;

            return true;
        }

        // not found and not added
        return false;
    }
}
