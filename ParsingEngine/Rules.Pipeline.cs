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
            var replaced = rx.Replace(input.Text, s.Options ?? "");
            return input with { Text = replaced };
        },
        "MapValue" => input =>
        {
            var from = s.From ?? "Text"; // Default to current text
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
            var from = s.From ?? "Text";
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
        "ExtractAllCapitalsFromStart" => input =>
        {
            var from = s.From ?? "Text";
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
            var from = s.From ?? "Text";
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
            var from = s.From ?? "Text";
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
            var from = s.From ?? "Text";
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
            var from = s.From ?? "Text";
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
            var from = s.From ?? "Text"; // Default to current text
            var to = s.To ?? "";
            
            var sourceValue = from.Equals("Text", StringComparison.OrdinalIgnoreCase) 
                ? input.Text 
                : input.Captures.TryGetValue(from, out var capValue) ? capValue : "";
                
            var caps = new Dictionary<string, string>(input.Captures, StringComparer.OrdinalIgnoreCase);
            caps[$"assign:{to}"] = sourceValue;
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
