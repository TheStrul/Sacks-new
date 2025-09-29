namespace ParsingEngine;

using System.Text.RegularExpressions;

/// <summary>
/// Base class for chain actions providing common input/output storage.
/// Concrete actions should inherit and call base constructor to set the keys.
/// </summary>
public abstract class BaseAction : IChainAction
{
    protected readonly string input;
    protected readonly string output;
    protected readonly bool assign;
    protected readonly string? condition;
    


    protected BaseAction(string input, string output, bool assign = false, string? condition = null)
    {
        this.input = input;
        this.output = output;
        this.condition = condition;
        this.assign = assign;
    }

    // Derived types must provide Op and Execute
    public abstract string Op { get; }
    public virtual bool Execute(IDictionary<string, string> bag, CellContext ctx)
    {
        if (string.IsNullOrEmpty(condition) == false)
        {
            bool cond = EvaluateCondition(bag);
            if (!cond)
                return false;
        }
        return true;
    }

    private bool EvaluateCondition(IDictionary<string, string> bag)
    {
        if (string.IsNullOrWhiteSpace(condition)) return false;
        var s = condition.Trim();

        // Try equality
        var eqIdx = s.IndexOf("==", StringComparison.Ordinal);
        if (eqIdx >= 0)
        {
            var left = s[..eqIdx].Trim();
            var right = s[(eqIdx + 2)..].Trim();
            var lv = ResolveToken(left, bag);
            var rv = ResolveToken(right, bag);
            return string.Equals(lv, rv, StringComparison.Ordinal);
        }

        var neqIdx = s.IndexOf("!=", StringComparison.Ordinal);
        if (neqIdx >= 0)
        {
            var left = s[..neqIdx].Trim();
            var right = s[(neqIdx + 2)..].Trim();
            var lv = ResolveToken(left, bag);
            var rv = ResolveToken(right, bag);
            return !string.Equals(lv, rv, StringComparison.Ordinal);
        }

        // unsupported
        return false;
    }

    private static string ResolveToken(string token, IDictionary<string, string> bag)
    {
        switch (token.ToLower())
        {
            case "true":
            case "false":
                return token;
        }
        if (string.IsNullOrWhiteSpace(token)) return string.Empty;
        // quoted string
        if ((token.StartsWith("\"") && token.EndsWith("\"")) || (token.StartsWith("'") && token.EndsWith("'")))
        {
            return token[1..^1];
        }

        // numeric literal
        if (int.TryParse(token, out _)) return token;

        // array access like Name[0]
        var m = Regex.Match(token, "^(.+?)\\[(\\d+)\\]$");
        if (m.Success)
        {
            var name = m.Groups[1].Value;
            var idx = int.Parse(m.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
            var key = $"{name}[{idx}]";
            if (bag.TryGetValue(key, out var v)) return v ?? string.Empty;
            return string.Empty;
        }

        // Length accessor: Name.Length
        if (token.EndsWith(".Length", StringComparison.OrdinalIgnoreCase))
        {
            var name = token[..^".Length".Length];
            if (bag.TryGetValue($"{name}.Length", out var v)) return v ?? string.Empty;
            return string.Empty;
        }

        // direct lookup
        if (bag.TryGetValue(token, out var val)) return val ?? string.Empty;
        return string.Empty;
    }
}
