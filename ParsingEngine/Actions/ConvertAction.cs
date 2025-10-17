using System.Globalization;

namespace ParsingEngine;

/// <summary>
/// Converts a numeric input value using a scale factor when a unit key matches a specific unit.
/// Parameters:
/// - FromUnit: source unit name to check (case-insensitive)
/// - ToUnit: target unit name to set when conversion happens
/// - Factor: double scale to multiply the input by
/// - UnitKey: name of the key in bag holding the unit (optional). If omitted, conversion is unconditional.
/// - Round: none | int (round to nearest integer, AwayFromZero)
/// - SetUnit: true|false (default true) to set the UnitKey to ToUnit after conversion
///
/// Input: numeric key to read; Output: numeric key to write.
/// Reads from both 'key' and 'assign:key' for convenience.
/// </summary>
public sealed class ConvertAction : BaseAction
{
    public override string Op => "convert";

    private readonly string? _fromUnit;
    private readonly string? _toUnit;
    private readonly double _factor;
    private readonly string? _unitKey;
    private readonly string _roundMode;
    private readonly bool _setUnit;

    public ConvertAction(string fromKey, string toKey, bool assign, string? condition,
                         string? fromUnit, string? toUnit, double factor,
                         string? unitKey, string? roundMode, bool setUnit)
        : base(fromKey, toKey, assign, condition)
    {
        _fromUnit = string.IsNullOrWhiteSpace(fromUnit) ? null : fromUnit.Trim();
        _toUnit = string.IsNullOrWhiteSpace(toUnit) ? null : toUnit.Trim();
        _factor = factor;
        _unitKey = string.IsNullOrWhiteSpace(unitKey) ? null : unitKey.Trim();
        _roundMode = string.IsNullOrWhiteSpace(roundMode) ? "none" : roundMode.Trim().ToLowerInvariant();
        _setUnit = setUnit;
    }

    public override bool Execute(IDictionary<string, string> bag, CellContext ctx)
    {
        if (!base.Execute(bag, ctx)) return false;

        // Resolve numeric input: prefer exact key, then assign: prefix
        if (!TryGetValue(bag, base.input, out var raw)) return false;
        if (!double.TryParse(raw, NumberStyles.Float, ctx.Culture ?? CultureInfo.InvariantCulture, out var n)) return false;

        // Check unit match if UnitKey provided
        bool shouldConvert = true;
        string? unitKeyActual = null;
        if (!string.IsNullOrWhiteSpace(_unitKey))
        {
            unitKeyActual = ResolveKey(bag, _unitKey!);
            if (!TryGetValue(bag, unitKeyActual, out var unitVal)) return false;
            if (!string.IsNullOrWhiteSpace(_fromUnit))
            {
                shouldConvert = string.Equals(unitVal?.Trim(), _fromUnit, StringComparison.OrdinalIgnoreCase);
            }
        }

        if (!shouldConvert) return false;

        var converted = n * _factor;
        if (_roundMode == "int")
        {
            converted = Math.Round(converted, 0, MidpointRounding.AwayFromZero);
        }

        var outStr = FormatNumber(converted, ctx.Culture);

        // Write numeric result
        if (base.assign)
            bag[$"assign:{base.output}"] = outStr;
        else
            bag[base.output] = outStr;

        // Set unit if requested
        if (_setUnit && !string.IsNullOrWhiteSpace(_toUnit) && !string.IsNullOrWhiteSpace(unitKeyActual))
        {
            bag[$"assign:{unitKeyActual}"] = _toUnit!;
        }

        return true;
    }

    private static string FormatNumber(double value, CultureInfo? culture)
    {
        // Use invariant rounded format, but with culture if provided for decimal separator
        culture ??= CultureInfo.InvariantCulture;
        // If it's integer, format without decimals
        if (Math.Abs(value % 1d) < 1e-9)
        {
            return ((long)value).ToString(culture);
        }
        return value.ToString("0.##", culture);
    }

    private static bool TryGetValue(IDictionary<string, string> bag, string key, out string s)
    {
        if (bag.TryGetValue(key, out s!)) return true;
        var alt = $"assign:{key}";
        if (bag.TryGetValue(alt, out s!)) return true;
        s = string.Empty; return false;
    }

    private static string ResolveKey(IDictionary<string, string> bag, string key)
    {
        // If exact exists, use it; otherwise return key (caller will try assign: too)
        return bag.ContainsKey(key) ? key : key;
    }
}
