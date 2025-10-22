using System.Globalization;

namespace ParsingEngine;

/// <summary>
/// Transforms input string casing. Supported modes: title | upper | lower.
/// When mode is 'title', converts to lower-case first, then applies TextInfo.ToTitleCase.
/// Optional parameter 'Culture' can override the culture; defaults to ctx.Culture.
/// </summary>
public sealed class CaseAction : BaseAction
{
    public override string Op => "case";

    private readonly string _mode;
    private readonly string? _cultureName;

    public CaseAction(string fromKey, string toKey, bool assign, string? condition, string mode, string? cultureName)
        : base(fromKey, toKey, assign, condition)
    {
        _mode = string.IsNullOrWhiteSpace(mode) ? "title" : mode.Trim();
        _cultureName = string.IsNullOrWhiteSpace(cultureName) ? null : cultureName.Trim();
    }

    public override bool Execute(CellContext ctx)
    {
        if (!base.Execute(ctx)) return false;

        if (!ctx.PropertyBag.Variables.TryGetValue(input, out var value) || value is null)
        {
            return false;
        }

        var culture = !string.IsNullOrWhiteSpace(_cultureName)
            ? new CultureInfo(_cultureName!)
            : ctx.Culture ?? CultureInfo.InvariantCulture;

        string result = value;
        switch (_mode.ToLowerInvariant())
        {
            case "upper":
                result = value.ToUpper(culture);
                break;
            case "lower":
                result = value.ToLower(culture);
                break;
            case "title":
            default:
                // Normalize to lower first so ToTitleCase yields predictable capitalization
                var lower = value.ToLower(culture);
                result = culture.TextInfo.ToTitleCase(lower);
                break;
        }

        if (assign)
        {
            ctx.PropertyBag.SetAssign(output, result ?? string.Empty);
        }
        else
        {
            ctx.PropertyBag.SetVariable(output, result ?? string.Empty);
        }

        return true;
    }
}
