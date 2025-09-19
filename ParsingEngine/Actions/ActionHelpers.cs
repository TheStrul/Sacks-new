using System;
using System.Collections.Generic;
using System.Globalization;

namespace ParsingEngine;

internal static class ActionHelpers
{
    public static void WriteListOutput(IDictionary<string, string> bag, string baseKey, string cleaned, IList<string>? results, bool valid, bool appendCleanToResults = false)
    {
        if (bag == null) throw new ArgumentNullException(nameof(bag));
        if (baseKey == null) throw new ArgumentNullException(nameof(baseKey));
        bag[$"{baseKey}.Clean"] = cleaned ?? string.Empty;
        var count = results?.Count ?? 0;
        bag[$"{baseKey}.Length"] = count.ToString(CultureInfo.InvariantCulture);
        bag[$"{baseKey}.Status"] = valid ? "true" : "false";
        if (results != null && results.Count > 0)
        {
            for (int i = 0; i < results.Count; i++)
            {
                bag[$"{baseKey}[{i}]"] = results[i] ?? string.Empty;
            }
        }

        if (appendCleanToResults)
        {
            // place cleaned value immediately after the last result (index = 3 + count)
            bag[$"{baseKey}[{count}]"] = cleaned ?? string.Empty;
        }
    }
}
