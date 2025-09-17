using System.Globalization;
using System.Text.RegularExpressions;

namespace ParsingEngine;

public sealed class MultiCaptureRegexRule : IRule
{
    public string Id { get; }
    public int Priority { get; }

    private readonly Regex _regex;
    private readonly Dictionary<string,string> _assign;

    public MultiCaptureRegexRule(RuleConfig rc)
    {
        ArgumentNullException.ThrowIfNull(rc);
        Id = rc.Id; Priority = rc.Priority;
        _regex = new Regex(rc.Pattern ?? "", RegexOptions.Compiled);
        _assign = rc.Assign ?? new();
    }

    public RuleExecutionResult Execute(CellContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        if (string.IsNullOrWhiteSpace(ctx.Raw)) return new(false, new());
        var m = _regex.Match(ctx.Raw.Trim());
        if (!m.Success) return new(false, new());

        var list = new List<Assignment>();
        foreach (var kvp in _assign)
        {
            var parts = kvp.Key.Split("->");
            var capture = parts[0];
            var property = parts[1];
            var converter = kvp.Value; // e.g., "decimal", "currencyNormalize"
            var rawValue = m.Groups[capture].Value;

            object? final = converter switch
            {
                "decimal" => decimal.Parse(rawValue.Replace(',', '.'), CultureInfo.InvariantCulture),
                "unitNormalize" => NormalizeUnit(rawValue),
                "currencyNormalize" => NormalizeCurrency(rawValue),
                _ => rawValue
            };
            list.Add(new Assignment(property, final, Id));
        }
        return new(true, list);
    }

    private static string NormalizeUnit(string u) => u.Trim().ToLowerInvariant() switch
    {
        "ml" or "ml" or "mL" or "ML" => "ml",
        _ => u.Trim()
    };

    private static string NormalizeCurrency(string c) => c.Trim().ToUpperInvariant() switch
    {
        "₪" => "ILS",
        "$" => "USD",
        "€" => "EUR",
        _ => c.Trim().ToUpperInvariant()
    };
}
