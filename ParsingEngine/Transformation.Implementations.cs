using System.Globalization;
using System.Text.RegularExpressions;

namespace ParsingEngine.Transformation;

/// <summary>
/// Capitalizes text using title case
/// </summary>
public sealed class CapitalizeTransformation : ITransformation
{
    public string Transform(string input, TransformationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (string.IsNullOrWhiteSpace(input)) return input;
        
        var culture = CultureInfo.GetCultureInfo(context.Culture);
        return culture.TextInfo.ToTitleCase(input.ToLowerInvariant());
    }
}

/// <summary>
/// Removes symbols and non-alphanumeric characters, keeping spaces
/// </summary>
public sealed class RemoveSymbolsTransformation : ITransformation
{
    public string Transform(string input, TransformationContext context)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;
        
        return Regex.Replace(input, @"[^\w\s]", "").Trim();
    }
}

/// <summary>
/// Maps values using lookup tables
/// </summary>
public sealed class MapValueTransformation : ITransformation
{
    public string Transform(string input, TransformationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (string.IsNullOrWhiteSpace(input)) return input;

        // Get table name from parameters
        if (!context.RowProperties.TryGetValue("param:table", out var tableObj) || tableObj == null)
            return input;

        var tableName = tableObj.ToString();
        if (string.IsNullOrEmpty(tableName)) return input;

        // Look up value in specified table
        if (context.Lookups.TryGetValue(tableName, out var lookup) &&
            lookup.TryGetValue(input, out var mappedValue))
        {
            return mappedValue;
        }

        // No mapping found, return original
        return input;
    }
}

/// <summary>
/// Extracts size and units from text like "100ml", "50 ML", "1.5L"
/// </summary>
public sealed class ExtractSizeAndUnitsTransformation : ITransformation
{
    public string Transform(string input, TransformationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (string.IsNullOrWhiteSpace(input)) return input;

        // Get output property names from parameters
        var sizeProperty = context.RowProperties.TryGetValue("param:sizeProperty", out var sizeObj) 
            ? sizeObj?.ToString() ?? "Size" : "Size";
        var unitsProperty = context.RowProperties.TryGetValue("param:unitsProperty", out var unitsObj) 
            ? unitsObj?.ToString() ?? "Units" : "Units";

        // Regex to extract size and units
        var match = Regex.Match(input, @"([0-9]+(?:\.[0-9]+)?)\s*([a-zA-Z]+)", RegexOptions.IgnoreCase);
        
        if (match.Success)
        {
            // Store extracted values in context for other transformations to use
            context.RowProperties[sizeProperty] = match.Groups[1].Value;
            context.RowProperties[unitsProperty] = match.Groups[2].Value.ToUpperInvariant();
            
            // Return the size value as the transformed result
            return match.Groups[1].Value;
        }
        else
        {
            // Try to extract just numbers
            var numberMatch = Regex.Match(input, @"([0-9]+(?:\.[0-9]+)?)");
            if (numberMatch.Success)
            {
                context.RowProperties[sizeProperty] = numberMatch.Groups[1].Value;
                context.RowProperties[unitsProperty] = "";
                return numberMatch.Groups[1].Value;
            }
        }

        return input;
    }
}

/// <summary>
/// Trims whitespace from input
/// </summary>
public sealed class TrimTransformation : ITransformation
{
    public string Transform(string input, TransformationContext context)
    {
        return input?.Trim() ?? "";
    }
}

/// <summary>
/// Converts text to uppercase
/// </summary>
public sealed class ToUpperTransformation : ITransformation
{
    public string Transform(string input, TransformationContext context)
    {
        return input?.ToUpperInvariant() ?? "";
    }
}

/// <summary>
/// Converts text to lowercase
/// </summary>
public sealed class ToLowerTransformation : ITransformation
{
    public string Transform(string input, TransformationContext context)
    {
        return input?.ToLowerInvariant() ?? "";
    }
}