using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;

namespace ParsingEngine;

public sealed class PipelineRule : IRule
{
    public string Id { get; }
    public int Priority { get; }

    private readonly List<Func<TransformResult, TransformResult>> _steps;
    private readonly Dictionary<string, Dictionary<string, string>> _lookups;

    public PipelineRule(RuleConfig rc, ParserConfig config)
    {
        ArgumentNullException.ThrowIfNull(rc);
        ArgumentNullException.ThrowIfNull(config);
        Id = rc.Id; Priority = rc.Priority;
        _lookups = config.Lookups;
        _steps = new();
        foreach (var s in rc.Steps ?? new())
            _steps.Add(BuildStep(s));
    }

    public RuleExecutionResult Execute(CellContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        if (string.IsNullOrWhiteSpace(ctx.Raw)) return new(false, new());
        var state = new TransformResult(ctx.Raw!, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

        foreach (var step in _steps)
            state = step(state);

        var assigns = state.Captures
            .Where(kv => kv.Key.StartsWith("assign:", StringComparison.OrdinalIgnoreCase))
            .Select(kv => new Assignment(kv.Key["assign:".Length..], kv.Value, Id))
            .ToList();

        return new(assigns.Any(), assigns);
    }

    private Func<TransformResult, TransformResult> BuildStep(PipelineStep s) => s.Op switch
    {
        "UnicodeNormalize" => input =>
        {
            var form = s.Form?.ToUpperInvariant() switch
            {
                "FORMKC" => NormalizationForm.FormKC,
                "FORMKD" => NormalizationForm.FormKD,
                "FORMC"  => NormalizationForm.FormC,
                "FORMD"  => NormalizationForm.FormD,
                _ => NormalizationForm.FormKC
            };
            return input with { Text = input.Text.Normalize(form) };
        },
        "NormalizeWhitespace" => input =>
        {
            var t = Regex.Replace(input.Text, "\\s+", " ").Trim();
            return input with { Text = t };
        },
        "ToUpper" => input => input with { Text = input.Text.ToUpperInvariant() },
        "ToLower" => input => input with { Text = input.Text.ToLowerInvariant() },
        "RegexExtract" => input =>
        {
            var rx = new Regex(s.Pattern ?? "", RegexOptions.Compiled | ToOptions(s.Options));
            var m = rx.Match(input.Text);
            if (!m.Success) return input;
            var caps = new Dictionary<string, string>(input.Captures, StringComparer.OrdinalIgnoreCase);
            foreach (var name in rx.GetGroupNames())
            {
                if (int.TryParse(name, out _)) continue;
                caps[name] = m.Groups[name].Value;
            }
            return input with { Captures = caps };
        },
        "RegexReplace" => input =>
        {
            var rx = new Regex(s.Pattern ?? "", RegexOptions.Compiled | ToOptions(s.Options));
            var replaced = rx.Replace(input.Text, s.Options ?? "");
            return input with { Text = replaced };
        },
        _ => input => input
    };

    private static RegexOptions ToOptions(string? options)
    {
        if (string.IsNullOrWhiteSpace(options)) return RegexOptions.None;
        var opts = RegexOptions.None;
        if (options.Contains("IgnoreCase", StringComparison.OrdinalIgnoreCase)) opts |= RegexOptions.IgnoreCase;
        if (options.Contains("Singleline", StringComparison.OrdinalIgnoreCase)) opts |= RegexOptions.Singleline;
        return opts;
    }
}
