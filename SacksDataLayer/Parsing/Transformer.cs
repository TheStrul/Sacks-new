using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using SacksDataLayer.FileProcessing.Normalizers;

namespace SacksDataLayer.Parsing;

/// <summary>
/// Provides comprehensive text transformation and data extraction capabilities for parsing operations.
/// </summary>
public class Transformer
{
    private readonly ILogger? _logger;
    private readonly Dictionary<string, Dictionary<string, string>> _valueMappings;

    public Transformer(ILogger? logger = null, Dictionary<string, Dictionary<string, string>>? valueMappings = null)
    {
        _logger = logger;
        _valueMappings = valueMappings ?? new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
    }

    #region Simple Transformations

    /// <summary>
    /// Applies a list of transformations to a value sequentially.
    /// </summary>
    /// <param name="value">The value to transform</param>
    /// <param name="transformations">List of transformation strings</param>
    /// <param name="currentKey">Current property key for value mapping context</param>
    /// <returns>The transformed value</returns>
    public string ApplyTransformations(string value, List<string> transformations, string currentKey)
    {
        if (string.IsNullOrEmpty(value) || transformations == null || !transformations.Any())
            return value;

        foreach (var transformation in transformations)
        {
            var parts = transformation.Split(':', 2, StringSplitOptions.RemoveEmptyEntries);
            var transformType = parts[0].ToLowerInvariant();
            var parameters = parts.Length > 1 ? parts[1] : null;

            switch (transformType)
            {
                case "removesymbols": 
                    value = RemoveSymbols(value);
                    break;
                case "removespaces": 
                    value = RemoveSpaces(value);
                    break;
                case "upperwords": 
                    value = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(value.ToLowerInvariant()); 
                    break;
                case "extracttitlefromstart":
                    value = ExtractTitleFromStart(value);
                    break;
                case "capitalize": 
                    value = string.IsNullOrEmpty(value) ? value : 
                            char.ToUpperInvariant(value[0]) + (value.Length > 1 ? value.Substring(1).ToLowerInvariant() : ""); 
                    break;
                case "mapvalue":
                    value = MapValue(value, currentKey);
                    break;
            }
            break; // Only apply first transformation that matches
        }
        return value;
    }

    /// <summary>
    /// Normalizes a string for mapping lookups by removing punctuation and normalizing whitespace.
    /// </summary>
    public string NormalizeForMapping(string input)
    {
        return NormalizeForMappingStatic(input);
    }

    /// <summary>
    /// Static version of NormalizeForMapping for use in constructors.
    /// </summary>
    public static string NormalizeForMappingStatic(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var candidate = Regex.Replace(input.Trim().ToLowerInvariant(), "[\\p{P}\\p{S}]+", " ").Trim();
        candidate = Regex.Replace(candidate, "\\s+", " ").Trim();
        return candidate;
    }

    /// <summary>
    /// Normalizes decimal strings by handling various decimal separators and removing non-numeric characters.
    /// </summary>
    public string NormalizeDecimal(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var s = input.Trim();
        if (s.Contains(',') && s.Contains('.')) s = s.Replace(",", "");
        else if (s.Contains(',') && !s.Contains('.')) s = s.Replace(',', '.');
        s = Regex.Replace(s, @"[^0-9.]", "");
        return s;
    }

    /// <summary>
    /// Normalizes unit strings to standard forms.
    /// </summary>
    public string NormalizeUnit(string unit)
    {
        if (string.IsNullOrWhiteSpace(unit)) return string.Empty;
        unit = unit.ToLowerInvariant().Replace("\\s", "");
        return unit switch 
        { 
            "ml" => "ml", 
            "l" or "litre" or "litres" => "l", 
            "cl" => "cl", 
            "oz" or "floz" or "fl oz" => "fl oz", 
            "g" => "g", 
            "kg" => "kg", 
            "mg" => "mg", 
            "mcg" => "mcg", 
            _ => unit 
        };
    }

    /// <summary>
    /// Removes symbols from a string, keeping only digits, dots, and commas.
    /// </summary>
    /// <param name="value">The input string to process</param>
    /// <returns>String with only digits, dots, and commas</returns>
    public static string RemoveSymbols(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return Regex.Replace(value, @"[^\d.,]", "");
    }

    /// <summary>
    /// Removes all spaces from a string.
    /// </summary>
    /// <param name="value">The input string to process</param>
    /// <returns>String with all spaces removed</returns>
    public static string RemoveSpaces(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Replace(" ", "");
    }

    /// <summary>
    /// Maps a value using the configured value mappings for a specific property.
    /// </summary>
    /// <param name="value">The input value to map</param>
    /// <param name="currentKey">The property key to use for value mapping context</param>
    /// <returns>The mapped value if found, otherwise the original value</returns>
    public string MapValue(string value, string currentKey)
    {
        if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(currentKey))
            return value;

        try
        {
            var lookupKey = NormalizeForMapping(value);
            if (_valueMappings.TryGetValue(currentKey, out var map) && 
                map.TryGetValue(lookupKey, out var mapped))
            {
                return mapped;
            }
        }
        catch
        {
            // Return original value if mapping fails
        }

        return value;
    }

    #endregion

    #region Pattern Extraction

    /// <summary>
    /// Extracts the portion of the input text that follows a specified prefix pattern.
    /// </summary>
    /// <remarks>
    /// Behavior:
    /// - If either <paramref name="value"/> or <paramref name="pattern"/> is null/empty, the original <paramref name="value"/> is returned.
    /// - If the pattern contains a '*' character the method delegates to <see cref="ExtractAfterWildcardPattern"/> which
    ///   converts common wildcard styles into regular expressions and returns the matched capture group.
    /// - Otherwise the pattern is treated as one or more literal prefixes separated by '|'. The method returns the
    ///   substring of <paramref name="value"/> that comes after the first matching prefix (comparison is case-insensitive).
    ///
    /// Examples:
    /// - value = "Brand: ACME 100ml", pattern = "Brand:" => returns "ACME 100ml"
    /// - value = "SKU|12345|X", pattern = "SKU|" => returns "12345|X" (prefix matching is literal)
    /// - value = "ACME:Red:Large", pattern = "*:" => delegates to wildcard handler and can return the appropriate capture
    /// - value = "NoPrefixHere", pattern = "Prefix:" => returns the original value "NoPrefixHere"
    /// </remarks>
    /// <param name="value">Input string to search.</param>
    /// <param name="pattern">Prefix pattern or wildcard expression. Use '|' to separate alternative literal prefixes or '*' to indicate wildcard patterns.</param>
    /// <returns>The substring after the matched prefix, or the original value if no prefix matches.</returns>
    public string ExtractAfterPattern(string value, string pattern)
    {
        // Guard clauses: if no input or pattern provided return original value unchanged
        if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(pattern)) return value;

        // If pattern contains wildcard(s) delegate to the wildcard-aware extractor which builds a regex
        if (pattern.Contains("*")) return ExtractAfterWildcardPattern(value, pattern);
        
        // Otherwise treat the pattern as one or more literal prefixes separated by '|'
        var prefixes = pattern.Split('|', StringSplitOptions.RemoveEmptyEntries);
        foreach (var prefix in prefixes) 
        {
            var trimmed = prefix.Trim();
            if (trimmed.Length == 0) continue;

            // Case-insensitive prefix match; if matched, return the remainder of the string after the prefix
            if (value.StartsWith(trimmed, StringComparison.OrdinalIgnoreCase)) 
                return value.Substring(trimmed.Length).Trim();
        }

        // No prefix matched - return original value
        return value;
    }

    /// <summary>
    /// Extracts text after a wildcard pattern.
    /// </summary>
    public string ExtractAfterWildcardPattern(string value, string pattern)
    {
        var regexPattern = ConvertWildcardToRegex(pattern);
        try
        {
            var match = Regex.Match(value, regexPattern, RegexOptions.IgnoreCase);
            if (match.Success) 
                return match.Groups.Count > 1 ? match.Groups[1].Value.Trim() : match.Value.Trim();
        }
        catch (ArgumentException ex) 
        { 
            _logger?.LogWarning("Invalid regex pattern '{RegexPattern}' for wildcard '{WildcardPattern}': {ErrorMessage}", 
                               regexPattern, pattern, ex.Message); 
        }
        return value;
    }

    /// <summary>
    /// Converts wildcard patterns to regex patterns.
    /// </summary>
    public string ConvertWildcardToRegex(string wildcardPattern)
    {
        return wildcardPattern switch
        {
            "*:" => @"^[^:]*:([^:]+):",
            "*:*" => @"^[^:]*:([^:]+):.*",
            "after:" => @"^[^:]*:(.+)$",
            "between:|" => @"^[^|]*\|([^|]+)\|",
            "between:;" => @"^[^;]*;([^;]+);",
            "prefix:*" => @"^[A-Z]+:(.+)$",
            _ => HandleCustomWildcardPattern(wildcardPattern)
        };
    }

    /// <summary>
    /// Handles custom wildcard patterns.
    /// </summary>
    public string HandleCustomWildcardPattern(string pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        
        if (pattern.StartsWith("between:") && pattern.Length > 8)
        {
            var delimiter = pattern.Substring(8, 1);
            var escapedDelimiter = Regex.Escape(delimiter);
            return $@"^[^{escapedDelimiter}]*{escapedDelimiter}([^{escapedDelimiter}]+){escapedDelimiter}";
        }
        var regexPattern = Regex.Escape(pattern).Replace(@"\*", "(.*)");
        return regexPattern;
    }

    #endregion

    #region Value Extraction

    /// <summary>
    /// Extracts price and currency from a string value.
    /// </summary>
    public (string Price, string Currency) ExtractPriceAndCurrency(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return (string.Empty, string.Empty);
        
        // Match common currency symbols or 3-letter ISO codes
        var currencyMatch = Regex.Match(value, @"(\u20AC|\$|£|¥|USD|EUR|GBP|CHF|PLN|CZK|HUF|RON|BGN|HRK|DKK|SEK|NOK)", RegexOptions.IgnoreCase);
        var currency = currencyMatch.Success ? currencyMatch.Value.Trim() : string.Empty;
        
        var cleaned = Regex.Replace(value, @"(?i)per|each|/unit|/piece|pcs", "");
        cleaned = Regex.Replace(cleaned, @"(\u20AC|\$|£|¥|USD|EUR|GBP|CHF|PLN|CZK|HUF|RON|BGN|HRK|DKK|SEK|NOK)", string.Empty, RegexOptions.IgnoreCase);
        cleaned = Regex.Replace(cleaned, @"[^0-9.,]", "").Trim();
        var normalized = NormalizeDecimal(cleaned);
        
        return (normalized, currency);
    }

    /// <summary>
    /// Extracts size and units from a string value.
    /// </summary>
    public (string Size, string Unit) ExtractSizeAndUnits(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return (string.Empty, string.Empty);
        
        var match = Regex.Match(value, @"(?i)([0-9]+(?:[.,][0-9]+)?)\s*(ml|l|litre|litres|cl|oz|fl\s*oz|g|kg|mg|mcg)\b");
        if (match.Success) 
            return (NormalizeDecimal(match.Groups[1].Value), NormalizeUnit(match.Groups[2].Value.ToLowerInvariant()));
        
        var numMatch = Regex.Match(value, @"([0-9]+(?:[.,][0-9]+)?)");
        if (numMatch.Success) 
        { 
            var size = NormalizeDecimal(numMatch.Groups[1].Value); 
            var unitMatch = Regex.Match(value, @"(?i)(ml|l|litre|litres|cl|oz|fl\s*oz|g|kg|mg|mcg)\b"); 
            var unit = unitMatch.Success ? NormalizeUnit(unitMatch.Value) : string.Empty; 
            return (size, unit); 
        }
        
        return (string.Empty, string.Empty);
    }

    /// <summary>
    /// Extracts all sizes from a complex string with multiple size notations.
    /// </summary>
    public SizeExtractionResult? ExtractAllSizes(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;

        // Look for parenthetical sections or the whole string if none
        var candidates = new List<string>();
        var parenRegex = new Regex("\\(([^)]*)\\)");
        foreach (Match m in parenRegex.Matches(input))
        {
            if (!string.IsNullOrWhiteSpace(m.Groups[1].Value)) candidates.Add(m.Groups[1].Value);
        }
        if (candidates.Count == 0) candidates.Add(input);

        // Tokenize by +, /, , and whitespace within candidates
        var tokenSplit = new[] { ",", "+", "/", " ", "-" };
        var pairs = new List<(string size, string unit)>();
        var sizeRegex = new Regex(@"(?<!\d)(\d{1,3}(?:[\.,]\d+)?)(?:\s?([a-zA-Zμμ]+))?", RegexOptions.Compiled);

        foreach (var cand in candidates)
        {
            var parts = cand.Split(tokenSplit, StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim());
            foreach (var part in parts)
            {
                // Try to find numeric+unit sequences in the token
                foreach (Match m in sizeRegex.Matches(part))
                {
                    var num = m.Groups[1].Value.Replace(',', '.').Trim();
                    var unit = m.Groups.Count > 2 ? m.Groups[2].Value.Trim().ToLowerInvariant() : string.Empty;
                    // Normalize unit common variants
                    if (unit == "g" || unit == "gr") unit = "g";
                    if (unit == "ml" || unit == "mL" || unit == "m l") unit = "ml";
                    if (unit == "l" || unit == "ltr" || unit == "lt") unit = "l";
                    if (unit == "μl" || unit == "ul") unit = "ul";
                    pairs.Add((num, unit));
                }
            }
        }

        if (pairs.Count == 0) return null;

        // Determine common unit if possible (non-empty and most frequent)
        var unitGroups = pairs.Where(p => !string.IsNullOrWhiteSpace(p.unit))
                              .GroupBy(p => p.unit)
                              .OrderByDescending(g => g.Count())
                              .ToList();
        var commonUnit = unitGroups.Count > 0 ? unitGroups.First().Key : string.Empty;

        var sizes = pairs.Select(p => p.size).ToList();
        var sizesList = string.Join(',', sizes);
        var aggregated = string.Join('+', sizes);

        return new SizeExtractionResult
        {
            First = sizes.FirstOrDefault() ?? string.Empty,
            SizesList = sizesList,
            Aggregated = aggregated,
            Units = commonUnit
        };
    }

    /// <summary>
    /// Returns the longest prefix (from the start) consisting of space-separated valid words.
    /// A valid word has only A–Z uppercase letters,"&" or ".".
    /// A single charecter word witch is A-Z is also condiderd valid.
    /// A single "&" is condidered valid only if it is not the first or the last word.    
    /// A single "." is condidered valid only if it is not the first word.
    /// </summary>
    /// <example
    /// "ACME & Co. Deluxe 100ml" => "ACME"
    /// "ACME & CO. Deluxe 100ml" => "ACME & CO."
    /// "ACME & Co. Ltd." => "ACME"
    /// "Hello TO YOU" => ""
    /// "A & B C" => "A & B C"
    /// "A & B C D &" => "A & B C D"
    /// "A&B Good" => "A&B"
    public static string ExtractUpperWordsFromStart(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        var tokens = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var resultTokens = new List<string>();

        bool IsUpperToken(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;

            bool IsUpperSegment(string seg)
            {
                if (string.IsNullOrEmpty(seg)) return false;
                bool hasLetter = false;
                foreach (var ch in seg)
                {
                    if (ch >= 'A' && ch <= 'Z') hasLetter = true;
                    else if (ch == '.') continue;
                    else return false;
                }
                return hasLetter;
            }

            // If contains '&', ensure it's not leading/trailing and each segment around '&' is a valid uppercase segment
            if (s.Contains('&'))
            {
                if (s.StartsWith("&") || s.EndsWith("&")) return false;
                var parts = s.Split('&');
                foreach (var p in parts)
                {
                    if (!IsUpperSegment(p)) return false;
                }
                return true;
            }

            // No '&' - treat as normal segment
            return IsUpperSegment(s);
        }

        for (int i = 0; i < tokens.Length; i++)
        {
            var t = tokens[i];
            if (string.IsNullOrEmpty(t)) break;

            // Single-character tokens
            if (t.Length == 1)
            {
                var ch = t[0];
                if (ch >= 'A' && ch <= 'Z')
                {
                    // single uppercase letter is valid
                    resultTokens.Add(t);
                    continue;
                }

                if (ch == '&' || ch == '.')
                {
                    // '&' or '.' are valid only if they are between two valid uppercase tokens
                    if (i > 0 && i < tokens.Length - 1 && IsUpperToken(tokens[i - 1]) && IsUpperToken(tokens[i + 1]))
                    {
                        resultTokens.Add(t);
                        continue;
                    }
                    break;
                }

                // any other single char invalid
                break;
            }

            // Multi-character token: must be only A-Z and '.' and contain at least one letter
            if (!IsUpperToken(t)) break;

            resultTokens.Add(t);
        }

        return resultTokens.Count == 0 ? string.Empty : string.Join(' ', resultTokens);
    }
    
    /// <summary>
    /// Returns the longest prefix (from the start) consisting of space-separated valid words.
    /// A valid word must start with a capital letter or be a number (digits only)
    /// Valid chars are A–Z lowercase letters,"&" , "." and 0-9
    /// A single charecter word witch is A-Z is considerd valid only if it is not first or last word.
    /// A single "&" is condidered valid only if it is not the first or the last word.    
    /// A single "." is condidered valid only if it is not the first word.
    /// </summary>
    /// <example
    /// "ACME & Co. Deluxe 100ml" => ""
    /// "Acme & CO. Deluxe 100ml" => "Acme"
    /// "Acme & Co. Ltd." => "Acme & Co. Ltd."
    /// "Hello TO YOU" => "Hello"
    /// "A & B C" => ""
    /// "Ab & B . Dolch 22 To Us ." => "Ab & B . Dolch 22 To Us ."
    /// "A&B Good" => ""
    public static string ExtractTitleFromStart(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        var tokens = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var resultTokens = new List<string>();

        bool IsValidTitleWord(string token)
        {
            if (string.IsNullOrEmpty(token)) return false;
            
            // Check if it's all digits
            if (token.All(c => c >= '0' && c <= '9'))
            {
                return true;
            }

            // Must start with capital letter
            if (token.Length == 0 || !(token[0] >= 'A' && token[0] <= 'Z'))
            {
                return false;
            }

            // Check if it's all uppercase (which should be rejected based on examples)
            bool isAllUppercase = token.All(c => (c >= 'A' && c <= 'Z') || c == '&' || c == '.');
            if (isAllUppercase && token.Any(c => c >= 'A' && c <= 'Z'))
            {
                return false; // Reject all uppercase words like "ACME", "CO."
            }

            // Check all characters are valid: A-Z, a-z, 0-9, &, .
            foreach (var ch in token)
            {
                if (!((ch >= 'A' && ch <= 'Z') || 
                      (ch >= 'a' && ch <= 'z') || 
                      (ch >= '0' && ch <= '9') || 
                      ch == '&' || ch == '.'))
                {
                    return false;
                }
            }

            return true;
        }

        // First pass: identify which tokens could be valid if positioned correctly
        var tokenValidityStatus = new bool[tokens.Length];
        for (int i = 0; i < tokens.Length; i++)
        {
            var token = tokens[i];
            
            if (token.Length == 1)
            {
                var ch = token[0];
                
                // Single uppercase letter: valid only if not first or last word
                if (ch >= 'A' && ch <= 'Z')
                {
                    tokenValidityStatus[i] = i > 0 && i < tokens.Length - 1;
                }
                // Single "&": valid only if not first or last word
                else if (ch == '&')
                {
                    tokenValidityStatus[i] = i > 0 && i < tokens.Length - 1;
                }
                // Single ".": valid only if not first word
                else if (ch == '.')
                {
                    tokenValidityStatus[i] = i > 0;
                }
                // Single digit: valid
                else if (ch >= '0' && ch <= '9')
                {
                    tokenValidityStatus[i] = true;
                }
                else
                {
                    tokenValidityStatus[i] = false;
                }
            }
            else
            {
                tokenValidityStatus[i] = IsValidTitleWord(token);
            }
        }

        // Second pass: build result considering special single-character rules
        for (int i = 0; i < tokens.Length; i++)
        {
            var token = tokens[i];
            
            // For single characters with special rules, check if adjacent tokens are also valid
            if (token.Length == 1 && tokenValidityStatus[i])
            {
                var ch = token[0];
                
                if ((ch >= 'A' && ch <= 'Z') || ch == '&')
                {
                    // Need both previous and next to be valid words (not single chars)
                    if (i > 0 && i < tokens.Length - 1 && 
                        tokenValidityStatus[i - 1] && tokenValidityStatus[i + 1])
                    {
                        resultTokens.Add(token);
                        continue;
                    }
                    break;
                }
                
                if (ch == '.')
                {
                    // Just need to not be first
                    if (i > 0 && tokenValidityStatus[i - 1])
                    {
                        resultTokens.Add(token);
                        continue;
                    }
                    break;
                }
                
                if (ch >= '0' && ch <= '9')
                {
                    resultTokens.Add(token);
                    continue;
                }
            }
            else if (tokenValidityStatus[i])
            {
                resultTokens.Add(token);
            }
            else
            {
                break; // Stop at first invalid token
            }
        }

        return resultTokens.Count == 0 ? string.Empty : string.Join(' ', resultTokens);
    }

    #endregion

    #region Complex Transformations with Extraction

    /// <summary>
    /// Applies transformations that can extract multiple properties from a single value.
    /// Returns both the transformed value and any extracted properties.
    /// </summary>
    public (string TransformedValue, Dictionary<string, string>? ExtraProperties) ApplyTransformationsWithExtraction(
        string value, List<string> transformations, string? currentPropertyKey = null)
    {
        var extraProps = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var originalValue = value; // Keep the original value for extraction transformations

        if (transformations == null || !transformations.Any())
            return (value, extraProps.Count > 0 ? extraProps : null);

        foreach (var transformation in transformations)
        {
            // Check if transformation has parameters (separated by :)
            var parts = transformation.Split(':', 2, StringSplitOptions.RemoveEmptyEntries);
            var transformType = parts[0].ToLowerInvariant();
            var parameters = parts.Length > 1 ? parts[1] : null;

            switch (transformType)
            {
                case "extractsizeandunits":
                    var sizeResult = ExtractSizeAndUnits(originalValue);
                    if (!string.IsNullOrEmpty(sizeResult.Size))
                    {
                        extraProps["Size"] = sizeResult.Size;
                        if (currentPropertyKey?.Equals("Size", StringComparison.OrdinalIgnoreCase) == true)
                            value = sizeResult.Size;
                    }
                    if (!string.IsNullOrEmpty(sizeResult.Unit))
                    {
                        extraProps["Units"] = sizeResult.Unit;
                        if (currentPropertyKey?.Equals("Units", StringComparison.OrdinalIgnoreCase) == true)
                            value = sizeResult.Unit;
                    }
                    
                    // Handle parameters for custom property names
                    if (!string.IsNullOrEmpty(parameters))
                    {
                        var paramParts = parameters.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        if (paramParts.Length >= 1 && !string.IsNullOrEmpty(sizeResult.Size))
                        {
                            var sizeKey = paramParts[0].Trim();
                            extraProps[sizeKey] = sizeResult.Size;
                            extraProps.Remove("Size"); // Remove default key
                            if (currentPropertyKey?.Equals(sizeKey, StringComparison.OrdinalIgnoreCase) == true)
                                value = sizeResult.Size;
                        }
                        if (paramParts.Length >= 2 && !string.IsNullOrEmpty(sizeResult.Unit))
                        {
                            var unitKey = paramParts[1].Trim();
                            extraProps[unitKey] = sizeResult.Unit;
                            extraProps.Remove("Units"); // Remove default key
                            if (currentPropertyKey?.Equals(unitKey, StringComparison.OrdinalIgnoreCase) == true)
                                value = sizeResult.Unit;
                        }
                    }
                    break;

                case "extractpattern":
                    if (!string.IsNullOrWhiteSpace(parameters))
                    {
                        try
                        {
                            var match = Regex.Match(originalValue, parameters, RegexOptions.IgnoreCase);
                            if (match.Success)
                            {
                                // If named groups exist, use them
                                foreach (var groupName in match.Groups.Keys)
                                {
                                    if (int.TryParse(groupName, out _)) continue; // Skip numeric group names
                                    var g = match.Groups[groupName];
                                    if (g.Success && !string.IsNullOrWhiteSpace(g.Value))
                                    {
                                        extraProps[groupName] = g.Value.Trim();
                                        if (currentPropertyKey?.Equals(groupName, StringComparison.OrdinalIgnoreCase) == true)
                                            value = g.Value.Trim();
                                    }
                                }

                                // If no named groups, use first capture group
                                if (extraProps.Count == 0 && match.Groups.Count > 1)
                                {
                                    var extractedKey = currentPropertyKey ?? "Extracted";
                                    var extractedValue = match.Groups[1].Value.Trim();
                                    extraProps[extractedKey] = extractedValue;
                                    if (currentPropertyKey?.Equals(extractedKey, StringComparison.OrdinalIgnoreCase) == true)
                                        value = extractedValue;
                                }
                                
                                if (!extraProps.ContainsKey(currentPropertyKey ?? ""))
                                    value = match.Groups.Count > 1 ? match.Groups[1].Value.Trim() : match.Value.Trim();
                            }
                        }
                        catch (ArgumentException ex)
                        {
                            _logger?.LogWarning("Invalid regex pattern '{Pattern}': {ErrorMessage}", parameters, ex.Message);
                        }
                    }
                    break;

                case "extractpriceandcurrency":
                    var priceResult = ExtractPriceAndCurrency(originalValue);
                    if (!string.IsNullOrEmpty(priceResult.Price))
                    {
                        extraProps["Price"] = priceResult.Price;
                        if (currentPropertyKey?.Equals("Price", StringComparison.OrdinalIgnoreCase) == true)
                            value = priceResult.Price;
                    }
                    if (!string.IsNullOrEmpty(priceResult.Currency))
                    {
                        extraProps["Currency"] = priceResult.Currency;
                        if (currentPropertyKey?.Equals("Currency", StringComparison.OrdinalIgnoreCase) == true)
                            value = priceResult.Currency;
                    }
                    break;

                case "upperwords":
                    value = ExtractUpperWordsFromStart(value);
                    if (!string.IsNullOrEmpty(currentPropertyKey))
                    {
                        extraProps[currentPropertyKey] = value;
                    }
                    break;

                case "extracttitlefromstart":
                    value = ExtractTitleFromStart(value);
                    if (!string.IsNullOrEmpty(currentPropertyKey))
                    {
                        extraProps[currentPropertyKey] = value;
                    }
                    break;

                case "capitalize":
                    value = string.IsNullOrEmpty(value) ? value : 
                            char.ToUpperInvariant(value[0]) + (value.Length > 1 ? value.Substring(1).ToLowerInvariant() : "");
                    break;

                default:
                    // For other transformations, fall back to the simple transformation method
                    value = ApplyTransformations(value, new List<string> { transformation }, currentPropertyKey ?? string.Empty);
                    break;
            }
        }

        return (value, extraProps.Count > 0 ? extraProps : null);
    }

    #endregion

    #region Validation


    #endregion

    #region Utility Methods

    /// <summary>
    /// Removes the first occurrence of a substring from a source string.
    /// </summary>
    public static string RemoveFirstOccurrence(string source, string toRemove, bool ignoreCase = false)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(toRemove)) return source ?? string.Empty;
        
        try
        {
            var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            var idx = source.IndexOf(toRemove, comparison);
            if (idx < 0) return source;
            return source.Remove(idx, toRemove.Length);
        }
        catch 
        { 
            return source; 
        }
    }

    #endregion
}

/// <summary>
/// Result of extracting multiple sizes from a complex string.
/// </summary>
public sealed class SizeExtractionResult
{
    public string First { get; set; } = string.Empty;
    public string SizesList { get; set; } = string.Empty; // CSV of sizes
    public string Aggregated { get; set; } = string.Empty; // joined with '+'
    public string Units { get; set; } = string.Empty; // common unit if any
}