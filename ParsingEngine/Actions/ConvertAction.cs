using System.Globalization;

namespace ParsingEngine;

/// <summary>
/// Converts a numeric value from one unit to another using a multiplicative factor.
/// Typical use: convert Product.Size from OZ to ML with Factor=29.5735.
/// Parameters:
/// - FromUnit: optional; when provided, conversion runs only if current unit (read from UnitKey) matches.
/// - ToUnit: optional; if provided and SetUnit=true, the unit stored at UnitKey will be updated to this value.
/// - UnitKey: optional; bag key that contains the current unit (e.g., "Product.Units" or "Units").
/// - SetUnit: optional; default true; when true and ToUnit provided and UnitKey present, writes converted unit.
///
/// Notes:
/// - For FromUnit='oz' and ToUnit='ml', snaps common fragrance sizes to canonical ml (e.g., 3.4oz -> 100ml, 2.0oz -> 60ml) using a small tolerance.
/// </summary>
public sealed class ConvertAction : BaseAction
{
    public override string Op => "convert";

    private readonly string? _fromUnit;
    private readonly string? _toUnit;
    private readonly double _factor;
    private readonly string? _unitKey;
    private readonly int? _roundDecimals; // null => none
    private readonly bool _setUnit;

    public ConvertAction(
        string fromKey,
        string toKey,
        bool assign,
        string? condition,
        string? fromUnit,
        string? toUnit,
        double factor,
        string? unitKey,
        string? round,
        bool setUnit)
        : base(fromKey, toKey, assign, condition)
    {
        _fromUnit = string.IsNullOrWhiteSpace(fromUnit) ? null : fromUnit.Trim();
        _toUnit = string.IsNullOrWhiteSpace(toUnit) ? null : toUnit.Trim();
        _factor = factor == 0d ? 1d : factor;
        _unitKey = string.IsNullOrWhiteSpace(unitKey) ? null : unitKey.Trim();
        _roundDecimals = ParseRound(round);
        _setUnit = setUnit;
    }

    public override bool Execute(IDictionary<string, string> bag, CellContext ctx)
    {
        if (!base.Execute(bag, ctx)) return false;

        // Read numeric value from input (supports assigned or plain)
        if (!TryRead(bag, input, out var raw) || string.IsNullOrWhiteSpace(raw))
            return false;

        if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            return false;

        // Check unit condition if specified (supports assigned or plain)
        if (!string.IsNullOrWhiteSpace(_fromUnit) && !string.IsNullOrWhiteSpace(_unitKey))
        {
            if (!TryRead(bag, _unitKey!, out var currentUnit) ||
                !string.Equals((currentUnit ?? string.Empty).Trim(), _fromUnit, StringComparison.OrdinalIgnoreCase))
            {
                // Requested a specific FromUnit but current unit doesn't match -> no-op
                return false;
            }
        }

        var converted = value * _factor;

        // Snap canonical fragrance sizes for oz -> ml
        if (IsOzToMl())
        {
            converted = SnapOzToMl(value, converted);
        }
        else if (_roundDecimals.HasValue)
        {
            converted = Math.Round(converted, _roundDecimals.Value, MidpointRounding.AwayFromZero);
        }

        var convertedStr = converted.ToString("0.########", CultureInfo.InvariantCulture);

        if (assign)
        {
            bag[$"assign:{output}"] = convertedStr;
        }
        else
        {
            bag[output] = convertedStr;
        }

        // Optionally update unit (write to assigned form)
        if (_setUnit && !string.IsNullOrWhiteSpace(_toUnit) && !string.IsNullOrWhiteSpace(_unitKey))
        {
            bag[$"assign:{_unitKey}"] = _toUnit!;
        }

        return true;
    }

    private static bool TryRead(IDictionary<string, string> bag, string key, out string? value)
    {
        if (bag.TryGetValue($"assign:{key}", out var a)) { value = a; return true; }
        if (bag.TryGetValue(key, out var v)) { value = v; return true; }
        value = null; return false;
    }

    private bool IsOzToMl()
        => !string.IsNullOrWhiteSpace(_fromUnit) && !string.IsNullOrWhiteSpace(_toUnit)
           && string.Equals(_fromUnit, "oz", StringComparison.OrdinalIgnoreCase)
           && string.Equals(_toUnit, "ml", StringComparison.OrdinalIgnoreCase);

    private static double SnapOzToMl(double oz, double ml)
    {
        var pairs = new (double oz, int ml)[]
        {
            (0.5, 15),
            (1.0, 30),
            (1.7, 50),
            (1.69, 50),
            (2.0, 60),
            (2.5, 75),
            (3.3, 100),
            (3.4, 100),
            (6.7, 200)
        };

        const double tol = 0.06; // ~ ±0.06 oz tolerance (~1.77 ml)
        foreach (var p in pairs)
        {
            if (Math.Abs(oz - p.oz) <= tol)
                return p.ml;
        }

        // Otherwise round to nearest ml normally
        return Math.Round(ml, 0, MidpointRounding.AwayFromZero);
    }

    private static int? ParseRound(string? round)
    {
        if (string.IsNullOrWhiteSpace(round)) return null; // no rounding by default
        if (string.Equals(round, "none", StringComparison.OrdinalIgnoreCase)) return null;
        if (int.TryParse(round, NumberStyles.Integer, CultureInfo.InvariantCulture, out var d) && d >= 0)
            return d;
        return null;
    }
}
