using System.Globalization;

namespace ParsingEngine;

internal static class ActionHelpers
{
    public static void WriteListOutput(PropertyBag propertyBag, string baseKey, string cleaned, IList<string>? results, bool assign, bool isSingle)
    {
        if (propertyBag == null) throw new ArgumentNullException(nameof(propertyBag));
        if (baseKey == null) throw new ArgumentNullException(nameof(baseKey));

        var bag = propertyBag.Variables;

        // Always set .Clean in Variables
        bag[$"{baseKey}.Clean"] = cleaned ?? string.Empty;

        var count = results?.Count ?? 0;
        var valid = results != null && results.Count > 0;
        
        // Length and Valid in Variables
        bag[$"{baseKey}.Length"] = count.ToString(CultureInfo.InvariantCulture);
        bag[$"{baseKey}.Valid"] = valid ? "true" : "false";

        if (results != null && results.Count > 0)
        {
            for (int i = 0; i < results.Count; i++)
            {
                var key = isSingle ? baseKey : $"{baseKey}[{i}]";
                if (assign)
                {
                    // Write directly to Assignes
                    propertyBag.SetAssign(key, results[i] ?? string.Empty);
                }
                else
                {
                    // Write to Variables
                    bag[key] = results[i] ?? string.Empty;
                }
            }
        }       
    }
}
