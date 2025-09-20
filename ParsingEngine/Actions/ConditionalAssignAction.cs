using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ParsingEngine;

/// <summary>
/// ConditionalAssignAction: evaluates a simple condition expression against the current property bag
/// and when true copies the Input value to Output (behaves like a guarded assign).
/// Supported expressions (simple): "Left == Right" and "Left != Right" where Left/Right may be:
/// - a bag key (e.g. Parts.Length or Parts[0] or Text)
/// - a quoted string "literal"
/// - an integer literal
/// </summary>
public sealed class ConditionalAssignAction : IChainAction
{
    public string Op => "conditional";
    private readonly string _fromKey;
    private readonly string _toKey;
    private readonly string _condition;
    private readonly bool _doAssign;

    public ConditionalAssignAction(string fromKey, string toKey, string condition, bool assign)
    {
        _fromKey = fromKey;
        _toKey = toKey;
        _condition = condition;
        _doAssign = assign;
    }

    public bool Execute(IDictionary<string, string> bag, CellContext ctx)
    {
        if (bag == null) throw new ArgumentNullException(nameof(bag));

        bool cond = EvaluateCondition(bag);
        if (!cond)
            return false;

        if (bag.TryGetValue(_fromKey, out var val))
        {            
            if (string.IsNullOrEmpty(val))
                return false;
            if (_doAssign)
            {
                bag[$"assign:{_toKey}"] = val;
            }
            else
            {
                bag[$"{_toKey}"] = val;
            }
            return true;
        }
        return false;
    }

    private bool EvaluateCondition(IDictionary<string, string> bag)
    {
        if (string.IsNullOrWhiteSpace(_condition)) return false;
        var s = _condition.Trim();

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
