using System.Globalization;

namespace ParsingEngine;

internal static class ActionHelpers
{
    public static void WriteListOutput(IDictionary<string, string> bag, string baseKey, string cleaned, IList<string>? results, bool assign, bool isSingle)
    {
        if (bag == null) throw new ArgumentNullException(nameof(bag));
        if (baseKey == null) throw new ArgumentNullException(nameof(baseKey));

        // Always set .Clean
        bag[$"{baseKey}.Clean"] = cleaned ?? string.Empty;

        var count = results?.Count ?? 0;
        var valid = results != null && results.Count > 0;
        
        // Length should reflect only the real results count (not the appended clean element)
        bag[$"{baseKey}.Length"] = count.ToString(CultureInfo.InvariantCulture);
        bag[$"{baseKey}.Valid"] = valid ? "true" : "false";

        if (results != null && results.Count > 0)
        {
            for (int i = 0; i < results.Count; i++)
            {
                var key = isSingle ? baseKey : $"{baseKey}[{i}]";
                if (assign)
                {
                    bag[$"assign:{key}"] = results[i] ?? string.Empty;
                }
                else
                {
                    bag[key] = results[i] ?? string.Empty;
                }
            }
        }       
    }
}
