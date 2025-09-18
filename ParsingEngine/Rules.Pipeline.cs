using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;

namespace ParsingEngine;

public sealed class PipelineRule : IRule
{
    private readonly List<Func<TransformResult, TransformResult>> _steps;
    private readonly Dictionary<string, Dictionary<string, string>> _lookups;

    public PipelineRule(RuleConfig rc, ParserConfig config)
    {
        ArgumentNullException.ThrowIfNull(rc);
        ArgumentNullException.ThrowIfNull(config);
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
            .Select(kv => new Assignment(kv.Key["assign:".Length..], kv.Value, "Pipeline"))
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
        "Capitalize" => input =>
        {
            var t = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(input.Text.ToLowerInvariant());
            return input with { Text = t };
        },
        "RemoveSymbols" => input =>
        {
            // Remove common symbols and non-alphanumeric characters, keep spaces
            var t = Regex.Replace(input.Text, @"[^\w\s]", "").Trim();
            return input with { Text = t };
        },
        "Trim" => input => input with { Text = input.Text.Trim() },
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
            var replaced = rx.Replace(input.Text, s.Replacement ?? "");
            return input with { Text = replaced };
        },
        "RegexRemove" => input =>
        {
            var rx = new Regex(s.Pattern ?? "", RegexOptions.Compiled | ToOptions(s.Options));
            var removed = rx.Replace(input.Text, "");
            return input with { Text = removed };
        },
        "MapValue" => input =>
        {
            var from = s.From ?? ""; // No default
            var table = s.Table ?? "";
            var caseMode = s.CaseMode ?? "exact"; // Default to exact match
            var extractedOut = s.ExtractedOut; // Optional output parameter
            
            var sourceValue = from.Equals("Text", StringComparison.OrdinalIgnoreCase) 
                ? input.Text 
                : input.Captures.TryGetValue(from, out var capValue) ? capValue : "";
            
            var caps = new Dictionary<string, string>(input.Captures);
            string mappedValue = sourceValue; // Default to original value
            
            if (_lookups.TryGetValue(table, out var lookup))
            {
                string lookupKey = caseMode switch
                {
                    "lower" => sourceValue.ToLowerInvariant(),
                    "upper" => sourceValue.ToUpperInvariant(),
                    _ => sourceValue // "exact" or any other value
                };
                
                if (lookup.TryGetValue(lookupKey, out var mapped))
                {
                    mappedValue = mapped;
                }
            }
            
            // If ExtractedOut is specified, put result there; otherwise update Text
            if (!string.IsNullOrEmpty(extractedOut))
            {
                caps[extractedOut] = mappedValue;
                return input with { Captures = caps };
            }
            else
            {
                return input with { Text = mappedValue };
            }
        },
        "SplitSizeAndUnits" => input =>
        {
            var from = s.From ?? "";
            var valueOut = s.ValueOut ?? "Size";
            var unitOut = s.UnitOut ?? "Units";
            
            var sourceValue = from.Equals("Text", StringComparison.OrdinalIgnoreCase) 
                ? input.Text 
                : input.Captures.TryGetValue(from, out var capValue) ? capValue : "";
            
            var caps = new Dictionary<string, string>(input.Captures, StringComparer.OrdinalIgnoreCase);
            
            if (!string.IsNullOrEmpty(sourceValue))
            {
                // Regex to extract size and units (e.g., "100ml", "50 ML", "1.5L")
                var match = Regex.Match(sourceValue, @"([0-9]+(?:\.[0-9]+)?)\s*([a-zA-Z]+)", RegexOptions.IgnoreCase);
                
                if (match.Success)
                {
                    caps[valueOut] = match.Groups[1].Value;
                    caps[unitOut] = match.Groups[2].Value.ToUpperInvariant();
                }
                else
                {
                    // No match, try to extract just numbers
                    var numberMatch = Regex.Match(sourceValue, @"([0-9]+(?:\.[0-9]+)?)");
                    if (numberMatch.Success)
                    {
                        caps[valueOut] = numberMatch.Groups[1].Value;
                        caps[unitOut] = ""; // Empty unit
                    }
                }
            }
            return input with { Captures = caps };
        },
        "SplitByDelimiter" => input =>
        {
            var from = s.From ?? ""; // No default
            var delimiter = s.Delimiter ?? ":";
            var outputProperty = s.OutputProperty ?? "Parts";
            var expectedParts = s.ExpectedParts;
            var strict = s.Strict ?? false;
            
            var sourceValue = from.Equals("Text", StringComparison.OrdinalIgnoreCase) 
                ? input.Text 
                : input.Captures.TryGetValue(from, out var capValue) ? capValue : "";
            
            var caps = new Dictionary<string, string>(input.Captures, StringComparer.OrdinalIgnoreCase);
            
            if (!string.IsNullOrEmpty(sourceValue))
            {
                var parts = sourceValue.Split(delimiter, StringSplitOptions.None);
                
                // Validation logic
                if (expectedParts.HasValue)
                {
                    if (strict && parts.Length != expectedParts.Value)
                    {
                        // In strict mode, if parts count doesn't match expected, fail the operation
                        caps[$"{outputProperty}.Error"] = $"Expected {expectedParts.Value} parts, got {parts.Length}";
                        caps[$"{outputProperty}.Valid"] = "false";
                        return input with { Captures = caps };
                    }
                    else if (!strict && parts.Length < expectedParts.Value)
                    {
                        // In non-strict mode, pad with empty strings if we have fewer parts than expected
                        var paddedParts = new string[expectedParts.Value];
                        for (int i = 0; i < expectedParts.Value; i++)
                        {
                            paddedParts[i] = i < parts.Length ? parts[i] : "";
                        }
                        parts = paddedParts;
                    }
                    else if (!strict && parts.Length > expectedParts.Value)
                    {
                        // In non-strict mode, truncate to expected parts if we have too many
                        var truncatedParts = new string[expectedParts.Value];
                        Array.Copy(parts, truncatedParts, expectedParts.Value);
                        parts = truncatedParts;
                    }
                }
                
                // Store the parts
                for (int i = 0; i < parts.Length; i++)
                {
                    var trimmedPart = parts[i].Trim();
                    caps[$"{outputProperty}[{i}]"] = trimmedPart;
                }
                caps[$"{outputProperty}.Length"] = parts.Length.ToString();
                caps[$"{outputProperty}.Valid"] = "true";
            }
            else
            {
                // Handle empty input
                if (expectedParts.HasValue && strict)
                {
                    caps[$"{outputProperty}.Error"] = "Empty input in strict mode";
                    caps[$"{outputProperty}.Valid"] = "false";
                }
                else if (expectedParts.HasValue)
                {
                    // In non-strict mode with empty input, create empty parts
                    for (int i = 0; i < expectedParts.Value; i++)
                    {
                        caps[$"{outputProperty}[{i}]"] = "";
                    }
                    caps[$"{outputProperty}.Length"] = expectedParts.Value.ToString();
                    caps[$"{outputProperty}.Valid"] = "true";
                }
            }
            
            return input with { Captures = caps };
        },
        "ExtractAllCapitalsFromStart" => input =>
        {
            var from = s.From ?? "";
            var extractedOut = s.ExtractedOut ?? "Extracted";
            var remainingOut = s.RemainingOut ?? "Remaining";
            
            var sourceValue = from.Equals("Text", StringComparison.OrdinalIgnoreCase) 
                ? input.Text 
                : input.Captures.TryGetValue(from, out var capValue) ? capValue : "";
            
            var caps = new Dictionary<string, string>(input.Captures, StringComparer.OrdinalIgnoreCase);
            
            // Extract all capital words from the start (stop when we hit a word with lowercase)
            var words = sourceValue.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var brandParts = new List<string>();
            var remainingParts = new List<string>();
            
            // First pass: find where capitals end
            int lastCapitalIndex = -1;
            for (int i = 0; i < words.Length; i++)
            {
                if (Regex.IsMatch(words[i], @"^[A-Z&]+$"))
                {
                    lastCapitalIndex = i;
                }
                else
                {
                    break; // Stop at first non-capital word
                }
            }
            
            // Second pass: apply single-letter restriction
            for (int i = 0; i < words.Length; i++)
            {
                var word = words[i];
                bool isCapital = Regex.IsMatch(word, @"^[A-Z&]+$");
                bool isLastCapital = (i == lastCapitalIndex);
                bool isSingleLetter = word.Length == 1;
                
                if (isCapital && !(isSingleLetter && isLastCapital))
                {
                    brandParts.Add(word);
                }
                else
                {
                    // Add this word and all remaining words to remaining
                    remainingParts.AddRange(words.Skip(i));
                    break;
                }
            }
            
            caps[extractedOut] = string.Join(" ", brandParts);
            caps[remainingOut] = string.Join(" ", remainingParts);
            
            return input with { Captures = caps };
        },
        "ExtractSizeAndUnits" => input =>
        {
            var from = s.From ?? "";
            var sizeOut = s.SizeOut ?? "Size";
            var remainingOut = s.RemainingOut ?? "Remaining";
            var patterns = s.Patterns ?? new[] { @"\(([^)]*(?:\d+(?:\.\d+)?\s*(?:ml|oz|l|cl|fl\s*oz|floz)[^)]*)\)" }; // Default pattern for parentheses
            
            var sourceValue = from.Equals("Text", StringComparison.OrdinalIgnoreCase) 
                ? input.Text 
                : input.Captures.TryGetValue(from, out var capValue) ? capValue : "";
            
            var caps = new Dictionary<string, string>(input.Captures, StringComparer.OrdinalIgnoreCase);
            
            var text = sourceValue.Trim();
            string extractedSize = "";
            string remaining = text;
            
            // Try each pattern in order until we find a match
            foreach (var pattern in patterns)
            {
                var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    extractedSize = match.Groups[1].Value.Trim();
                    
                    // Remove the matched pattern from the text
                    remaining = Regex.Replace(text, pattern, " ", RegexOptions.IgnoreCase).Trim();
                    // Clean up multiple spaces
                    remaining = Regex.Replace(remaining, @"\s+", " ");
                    break;
                }
            }
            
            caps[remainingOut] = remaining;
            caps[sizeOut] = extractedSize;
            
            return input with { Captures = caps };
        },
        "ExtractMappedValue" => input =>
        {
            var from = s.From ?? "";
            var table = s.Table ?? "";
            var extractedOut = s.ExtractedOut ?? "Extracted";
            var remainingOut = s.RemainingOut ?? "Remaining";
            
            var sourceValue = from.Equals("Text", StringComparison.OrdinalIgnoreCase) 
                ? input.Text 
                : input.Captures.TryGetValue(from, out var capValue) ? capValue : "";
            
            var caps = new Dictionary<string, string>(input.Captures, StringComparer.OrdinalIgnoreCase);
            
            if (_lookups.TryGetValue(table, out var lookup))
            {
                string foundKey = "";
                string foundValue = "";
                
                // Find any key from the lookup table in the source text
                foreach (var kvp in lookup)
                {
                    if (sourceValue.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        foundKey = kvp.Key;
                        foundValue = kvp.Value;
                        break;
                    }
                }
                
                if (!string.IsNullOrEmpty(foundKey))
                {
                    caps[extractedOut] = foundValue;
                    // Remove the found key from the source text
                    var remaining = sourceValue.Replace(foundKey, "", StringComparison.OrdinalIgnoreCase).Trim();
                    // Clean up multiple spaces
                    remaining = Regex.Replace(remaining, @"\s+", " ");
                    caps[remainingOut] = remaining;
                }
                else
                {
                    caps[extractedOut] = "";
                    caps[remainingOut] = sourceValue.Trim();
                }
            }
            else
            {
                caps[extractedOut] = "";
                caps[remainingOut] = sourceValue.Trim();
            }
            
            return input with { Captures = caps };
        },
        "ExtractLastWord" => input =>
        {
            var from = s.From ?? "";
            var wordOut = s.WordOut ?? "LastWord";
            var remainingOut = s.RemainingOut ?? "Remaining";
            
            var sourceValue = from.Equals("Text", StringComparison.OrdinalIgnoreCase) 
                ? input.Text 
                : input.Captures.TryGetValue(from, out var capValue) ? capValue : "";
            
            var caps = new Dictionary<string, string>(input.Captures, StringComparer.OrdinalIgnoreCase);
            
            if (!string.IsNullOrWhiteSpace(sourceValue))
            {
                var words = sourceValue.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (words.Length > 0)
                {
                    caps[wordOut] = words[^1]; // Last word
                    caps[remainingOut] = words.Length > 1 
                        ? string.Join(" ", words[..^1]) // All but last word
                        : ""; // If only one word, remaining is empty
                }
                else
                {
                    caps[wordOut] = "";
                    caps[remainingOut] = "";
                }
            }
            else
            {
                caps[wordOut] = "";
                caps[remainingOut] = "";
            }
            
            return input with { Captures = caps };
        },
        "ExtractMappedWordAnywhere" => input =>
        {
            var from = s.From ?? "";
            var table = s.Table ?? "";
            var caseMode = s.CaseMode ?? "exact";
            var extractedOut = s.ExtractedOut ?? "ExtractedWord";
            var remainingOut = s.RemainingOut ?? "Remaining";
            
            var sourceValue = from.Equals("Text", StringComparison.OrdinalIgnoreCase) 
                ? input.Text 
                : input.Captures.TryGetValue(from, out var capValue) ? capValue : "";
            
            var caps = new Dictionary<string, string>(input.Captures, StringComparer.OrdinalIgnoreCase);
            
            string extractedWord = "";
            string remaining = sourceValue.Trim();
            
            if (!string.IsNullOrWhiteSpace(sourceValue) && _lookups.TryGetValue(table, out var lookup))
            {
                var words = sourceValue.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                
                for (int i = 0; i < words.Length; i++)
                {
                    var word = words[i];
                    string lookupKey = caseMode switch
                    {
                        "lower" => word.ToLowerInvariant(),
                        "upper" => word.ToUpperInvariant(),
                        _ => word // "exact" or any other value
                    };
                    
                    if (lookup.ContainsKey(lookupKey))
                    {
                        extractedWord = word;
                        // Remove this word from the remaining text
                        var remainingWords = words.Where((w, index) => index != i).ToArray();
                        remaining = string.Join(" ", remainingWords);
                        break;
                    }
                }
            }
            
            caps[extractedOut] = extractedWord;
            caps[remainingOut] = remaining;
            
            return input with { Captures = caps };
        },
        "Assign" => input =>
        {
            var from = s.From ?? ""; // No default
            var to = s.To ?? "";
            
            string sourceValue = "";
            
            if (from.Equals("Text", StringComparison.OrdinalIgnoreCase))
            {
                sourceValue = input.Text;
            }
            else if (from.Contains("[") && from.Contains("]"))
            {
                // Handle array indexing like "Parts[0]"
                var arrayMatch = Regex.Match(from, @"^(.+?)\[(\d+)\]$");
                if (arrayMatch.Success)
                {
                    var arrayName = arrayMatch.Groups[1].Value;
                    var index = int.Parse(arrayMatch.Groups[2].Value);
                    
                    if (input.Captures.TryGetValue($"{arrayName}[{index}]", out var arrayValue))
                    {
                        sourceValue = arrayValue;
                    }
                }
            }
            else
            {
                // First try with assign: prefix for intermediate captures
                if (input.Captures.TryGetValue($"assign:{from}", out var assignValue))
                {
                    sourceValue = assignValue;
                }
                else if (input.Captures.TryGetValue(from, out var capValue))
                {
                    sourceValue = capValue;
                }
                else
                {
                    sourceValue = "";
                }
            }
                
            var caps = new Dictionary<string, string>(input.Captures, StringComparer.OrdinalIgnoreCase);
            
            // Always use assign: prefix for final extraction by PipelineRule
            caps[$"assign:{to}"] = sourceValue;
            
            return input with { Captures = caps };
        },
        "ConditionalMapping" => input =>
        {
            var from = s.From ?? "";
            var mappings = s.Mappings ?? new List<SplitMapping>();
            
            string sourceValue = "";
            
            // Get source value
            if (input.Captures.TryGetValue($"assign:{from}", out var assignValue))
            {
                sourceValue = assignValue;
            }
            else if (input.Captures.TryGetValue(from, out var capValue))
            {
                sourceValue = capValue;
            }
            
            var caps = new Dictionary<string, string>(input.Captures, StringComparer.OrdinalIgnoreCase);
            
            if (string.IsNullOrWhiteSpace(sourceValue))
            {
                return input with { Captures = caps };
            }
            
            // Split by delimiter if present (configurable separator)
            var delimiter = s.Delimiter ?? "|";
            var parts = sourceValue.Split(delimiter, StringSplitOptions.RemoveEmptyEntries)
                                   .Select(p => p.Trim())
                                   .Where(p => !string.IsNullOrEmpty(p))
                                   .ToArray();
            
            // Process each mapping rule
            foreach (var mapping in mappings)
            {
                var lookupTable = mapping.Table;
                var targetProperty = mapping.AssignTo;
                
                if (string.IsNullOrEmpty(lookupTable) || string.IsNullOrEmpty(targetProperty))
                    continue;
                
                // Get the lookup table from config
                var lookup = _lookups?.GetValueOrDefault(lookupTable);
                if (lookup == null) continue;
                
                // Find first part that matches this lookup table
                bool foundMatch = false;
                foreach (var part in parts)
                {
                    var upperPart = part.ToUpperInvariant();
                    
                    if (lookup.TryGetValue(upperPart, out var mappedValue))
                    {
                        // Only assign if the mapped value is not empty
                        if (!string.IsNullOrEmpty(mappedValue))
                        {
                            caps[$"assign:{targetProperty}"] = mappedValue;
                        }
                        foundMatch = true;
                        break; // Stop at first match for this mapping
                    }
                }
                
                // Only apply fallback if no match was found AND fallback exists AND fallback is not empty
                if (!foundMatch && lookup.TryGetValue("", out var fallbackValue) && !string.IsNullOrEmpty(fallbackValue))
                {
                    caps[$"assign:{targetProperty}"] = fallbackValue;
                }
            }
            
            return input with { Captures = caps };
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
