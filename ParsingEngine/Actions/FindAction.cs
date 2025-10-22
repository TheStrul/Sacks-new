using System.Text.RegularExpressions;

namespace ParsingEngine;

public sealed class FindAction : BaseAction
{
    public override string Op => "find";
    private readonly string _pattern;
    private readonly string? _patternKey;
    private readonly List<string> _options;
    private readonly List<KeyValuePair<string,string>>? _lookupEntries;

    public FindAction(string fromKey, string toKey, string pattern, List<string> options, bool assign, string? condition, List<KeyValuePair<string,string>>? lookupEntries = null, string? patternKey = null) :
        base(fromKey, toKey, assign, condition)
    {
        _pattern = pattern ?? string.Empty;
        _patternKey = patternKey;
        _options = options ?? new List<string>();
        _lookupEntries = lookupEntries;
    }

    public override bool Execute(CellContext ctx)
    {
        if (base.Execute(ctx) == false) return false; // basic checks

        ctx.PropertyBag.Variables.TryGetValue(base.input, out var value);
        var input = value ?? string.Empty;

        var remove = _options.Any(o => string.Equals(o, "remove", StringComparison.OrdinalIgnoreCase));
        var ignoreCase = _options.Any(o => string.Equals(o, "ignorecase", StringComparison.OrdinalIgnoreCase));

        var mode = _options.FirstOrDefault(o => string.Equals(o, "all", StringComparison.OrdinalIgnoreCase)
                                              || string.Equals(o, "last", StringComparison.OrdinalIgnoreCase)
                                              || string.Equals(o, "first", StringComparison.OrdinalIgnoreCase))
                   ?? "first";

        try
        {
            // Prefer lookup-based matching when provided
            if (_lookupEntries != null && _lookupEntries.Count > 0)
            {
                return mode.ToLowerInvariant() switch
                {
                    "all" => HandleAllLookup(ctx, input, ignoreCase, remove, base.assign),
                    "last" => HandleLastLookup(ctx, input, ignoreCase, remove, base.assign),
                    _ => HandleFirstLookup(ctx, input, ignoreCase, remove, base.assign),
                };
            }

            // Dynamic pattern from bag/PropertyBag via PatternKey
            if (!string.IsNullOrWhiteSpace(_patternKey))
            {
                string? dyn = null;
                if (!ctx.PropertyBag.Variables.TryGetValue(_patternKey!, out dyn) || string.IsNullOrWhiteSpace(dyn))
                {
                    // Try PropertyBag values
                    ctx.PropertyBag.Assignes.TryGetValue(_patternKey!, out var dynObj);
                    dyn = dynObj?.ToString();
                }

                if (string.IsNullOrWhiteSpace(dyn) || string.IsNullOrEmpty(input))
                {
                    ActionHelpers.WriteListOutput(ctx.PropertyBag, base.output, input, null, false, true);
                    return false;
                }

                var pat = $"(?<!\\p{{L}})" + Regex.Escape(dyn!) + $"(?!\\p{{L}})";
                var rx = new Regex(pat, ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);

                return mode.ToLowerInvariant() switch
                {
                    "all" => remove ? RemoveAll(ctx, rx, input, base.assign) : FindAll(ctx, rx, input, base.assign),
                    "last" => remove ? RemoveLast(ctx, rx, input, base.assign) : FindLast(ctx, rx, input, base.assign),
                    _ => remove ? RemoveFirst(ctx, rx, input, base.assign) : FindFirst(ctx, rx, input, base.assign),
                };
            }

            // Static regex pattern
            if (string.IsNullOrEmpty(_pattern) || string.IsNullOrEmpty(input))
            {
                ActionHelpers.WriteListOutput(ctx.PropertyBag, base.output, input, null, false, true);
                return false;
            }

            var rxOpts = ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
            var rxStatic = new Regex(_pattern, rxOpts);

            return mode.ToLowerInvariant() switch
            {
                "all" => remove ? RemoveAll(ctx, rxStatic, input, base.assign) : FindAll(ctx, rxStatic, input, base.assign),
                "last" => remove ? RemoveLast(ctx, rxStatic, input, base.assign) : FindLast(ctx, rxStatic, input, base.assign),
                _ => remove ? RemoveFirst(ctx, rxStatic, input, base.assign) : FindFirst(ctx, rxStatic, input, base.assign),
            };
        }
        catch
        {
            ActionHelpers.WriteListOutput(ctx.PropertyBag, base.output, input, null, false, true);
            return false;
        }
    }

    private void AddTrace(CellContext ctx, string text)
    {
        // Tracing removed - no-op
    }

    // Publicly requested small methods (private) delegating to core handlers
    private bool FindAll(CellContext ctx, Regex rx, string input, bool assignFlag) => HandleAll(ctx, rx, input, assignFlag, remove: false);
    private bool RemoveAll(CellContext ctx, Regex rx, string input, bool assignFlag) => HandleAll(ctx, rx, input, assignFlag, remove: true);
    private bool FindLast(CellContext ctx, Regex rx, string input, bool assignFlag) => HandleLast(ctx, rx, input, assignFlag, remove: false);
    private bool RemoveLast(CellContext ctx, Regex rx, string input, bool assignFlag) => HandleLast(ctx, rx, input, assignFlag, remove: true);
    private bool FindFirst(CellContext ctx, Regex rx, string input, bool assignFlag) => HandleFirst(ctx, rx, input, assignFlag, remove: false);
    private bool RemoveFirst(CellContext ctx, Regex rx, string input, bool assignFlag) => HandleFirst(ctx, rx, input, assignFlag, remove: true);

    private static string GetMatchResult(Match m, Regex rx)
    {
        // Prefer a named group 'size' when present, otherwise prefer the first non-numeric group name.
        var names = rx.GetGroupNames();
        if (names.Contains("size") && m.Groups["size"].Success)
            return m.Groups["size"].Value;

        foreach (var name in names)
        {
            if (int.TryParse(name, out _)) continue;
            if (m.Groups[name].Success) return m.Groups[name].Value;
        }

        return m.Value;
    }

    // Lookup-based handlers: prefer longest-key-first to avoid short-substring wins
    private IEnumerable<KeyValuePair<string,string>> OrderedLookupEntries(bool ignoreCase)
    {
        // Sort by key length descending; stable for equal lengths
        return _lookupEntries!
            .Where(kv => !string.IsNullOrEmpty(kv.Key))
            .OrderByDescending(kv => (kv.Key?.Length) ?? 0);
    }

    private bool HandleAllLookup(CellContext ctx, string input, bool ignoreCase, bool remove, bool assignFlag)
    {
        // trace lookup order
        AddTrace(ctx, $"FindAction ordered lookup keys: {string.Join(',', OrderedLookupEntries(ignoreCase).Select(kv => kv.Key))}");

        var results = new List<string>();
        var opts = ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;

        foreach (var kv in OrderedLookupEntries(ignoreCase))
        {
            var key = kv.Key ?? string.Empty;
            try
            {
                var pat = $"(?<!\\p{{L}})" + Regex.Escape(key) + $"(?!\\p{{L}})";
                var rx = new Regex(pat, opts);
                if (rx.IsMatch(input))
                {
                    results.Add(kv.Value ?? string.Empty);
                    AddTrace(ctx, $"Matched key='{key}' -> value='{kv.Value}'");
                }
            }
            catch
            {
            }
        }

        if (results.Count == 0)
        {
            ActionHelpers.WriteListOutput(ctx.PropertyBag, base.output, input, null, false, true);
            return false;
        }

        if (remove)
        {
            // remove all matches of each key in lookup order, replacing with space
            var cleaned = input;
            foreach (var kv in OrderedLookupEntries(ignoreCase))
            {
                var key = kv.Key ?? string.Empty;
                try
                {
                    var pat = $"(?<!\\p{{L}})" + Regex.Escape(key) + $"(?!\\p{{L}})";
                    cleaned = Regex.Replace(cleaned, pat, " ", ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
                }
                catch { }
            }
            cleaned = Regex.Replace(cleaned, "\\s+", " ").Trim();
            ActionHelpers.WriteListOutput(ctx.PropertyBag, base.output, cleaned, results, assignFlag, false);
        }
        else
        {
            ActionHelpers.WriteListOutput(ctx.PropertyBag, base.output, input, results, assignFlag, false);
        }

        return true;
    }

    private bool HandleFirstLookup(CellContext ctx, string input, bool ignoreCase, bool remove, bool assignFlag)
    {
        AddTrace(ctx, $"FindAction ordered lookup keys: {string.Join(',', OrderedLookupEntries(ignoreCase).Select(kv => kv.Key))}");

        var opts = ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
        foreach (var kv in OrderedLookupEntries(ignoreCase))
        {
            var key = kv.Key ?? string.Empty;
            try
            {
                var pat = $"(?<!\\p{{L}})" + Regex.Escape(key) + $"(?!\\p{{L}})";
                var rx = new Regex(pat, opts);
                var match = rx.Match(input);
                if (match.Success)
                {
                    var mapped = kv.Value ?? string.Empty;
                    AddTrace(ctx, $"Matched key='{key}' -> value='{mapped}'");
                    if (remove)
                    {
                        var cleaned = input.Substring(0, match.Index) + " " + input.Substring(match.Index + match.Length);
                        cleaned = Regex.Replace(cleaned, "\\s+", " ").Trim();
                        ActionHelpers.WriteListOutput(ctx.PropertyBag, base.output, cleaned, new List<string> { mapped }, assignFlag, true);
                        return true;
                    }

                    ActionHelpers.WriteListOutput(ctx.PropertyBag, base.output, input, new List<string> { mapped }, assignFlag, true);
                    return true;
                }
            }
            catch { }
        }

        ActionHelpers.WriteListOutput(ctx.PropertyBag, base.output, input, null, false, true);
        return false;
    }

    private bool HandleLastLookup(CellContext ctx, string input, bool ignoreCase, bool remove, bool assignFlag)
    {
        AddTrace(ctx, $"FindAction ordered lookup keys: {string.Join(',', OrderedLookupEntries(ignoreCase).Select(kv => kv.Key))}");

        var opts = ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
        Match? lastMatch = null;
        string? lastMapped = null;

        foreach (var kv in OrderedLookupEntries(ignoreCase))
        {
            var key = kv.Key ?? string.Empty;
            try
            {
                var pat = $"(?<!\\p{{L}})" + Regex.Escape(key) + $"(?!\\p{{L}})";
                var rx = new Regex(pat, opts);
                var matches = rx.Matches(input).Cast<Match>().ToArray();
                if (matches.Length > 0)
                {
                    var m = matches[^1];
                    lastMatch = m;
                    lastMapped = kv.Value ?? string.Empty;
                    AddTrace(ctx, $"Candidate last: key='{key}' at {m.Index}-{m.Length} -> '{lastMapped}'");
                }
            }
            catch { }
        }

        if (lastMatch == null)
        {
            ActionHelpers.WriteListOutput(ctx.PropertyBag, base.output, input, null, false, true);
            return false;
        }

        if (remove)
        {
            var cleaned = input.Substring(0, lastMatch.Index) + " " + input.Substring(lastMatch.Index + lastMatch.Length);
            cleaned = Regex.Replace(cleaned, "\\s+", " ").Trim();
            ActionHelpers.WriteListOutput(ctx.PropertyBag, base.output, cleaned, new List<string> { lastMapped! }, assignFlag, true);
            return true;
        }

        ActionHelpers.WriteListOutput(ctx.PropertyBag, base.output, input, new List<string> { lastMapped! }, assignFlag, true);
        return true;
    }

    private bool HandleAll(CellContext ctx, Regex rx, string input, bool assignFlag, bool remove)
    {
        // Try primary regex first, but if it yields no matches and pattern contains \b try a Unicode-letter based boundary fallback
        Regex effectiveRx = rx;
        var matches = effectiveRx.Matches(input).Cast<Match>().ToArray();
        if (matches.Length == 0 && _pattern.Contains("\\b"))
        {
            try
            {
                var altPattern = _pattern.Replace("\\b", string.Empty);
                var altWrapped = @"(?<!\p{L})(?:" + altPattern + @")(?!\p{L})";
                var altRx = new Regex(altWrapped, rx.Options);
                var altMatches = altRx.Matches(input).Cast<Match>().ToArray();
                if (altMatches.Length > 0)
                {
                    effectiveRx = altRx;
                    matches = altMatches;
                }
            }
            catch
            {
                // ignore fallback errors and continue with original behavior
            }
        }

        if (matches.Length == 0)
        {
            ActionHelpers.WriteListOutput(ctx.PropertyBag, base.output, input, null, false, true);
            return false;
        }

        // Use group values when available (e.g., 'size') so results list contains clean values while the regex
        // match may include surrounding separators for removal.
        var results = matches.Select(m => GetMatchResult(m, effectiveRx)).ToList();

        if (remove)
        {
            var cleaned = effectiveRx.Replace(input, " ");
            cleaned = Regex.Replace(cleaned, "\\s+", " ").Trim();
            ActionHelpers.WriteListOutput(ctx.PropertyBag, base.output, cleaned, results, assignFlag, false);
        }
        else
        {
            ActionHelpers.WriteListOutput(ctx.PropertyBag, base.output, input, results, assignFlag, false);
        }

        return true;
    }

    private bool HandleLast(CellContext ctx, Regex rx, string input, bool assignFlag, bool remove)
    {
        Regex effectiveRx = rx;
        var matches = effectiveRx.Matches(input).Cast<Match>().ToArray();
        if (matches.Length == 0 && _pattern.Contains("\\b"))
        {
            try
            {
                var altPattern = _pattern.Replace("\\b", string.Empty);
                var altWrapped = @"(?<!\p{L})(?:" + altPattern + @")(?!\p{L})";
                var altRx = new Regex(altWrapped, rx.Options);
                var altMatches = altRx.Matches(input).Cast<Match>().ToArray();
                if (altMatches.Length > 0)
                {
                    effectiveRx = altRx;
                    matches = altMatches;
                }
            }
            catch
            {
            }
        }

        if (matches.Length == 0)
        {
            ActionHelpers.WriteListOutput(ctx.PropertyBag, base.output, input, null, false, true);
            return false;
        }

        var m = matches[^1];
        var result = GetMatchResult(m, effectiveRx);
        if (remove)
        {
            var cleaned = input.Substring(0, m.Index) + " " + input.Substring(m.Index + m.Length);
            cleaned = Regex.Replace(cleaned, "\\s+", " ").Trim();
            ActionHelpers.WriteListOutput(ctx.PropertyBag, base.output, cleaned, new List<string> { result }, assignFlag, true);
            return true;
        }

        ActionHelpers.WriteListOutput(ctx.PropertyBag, base.output, input, new List<string> { result }, assignFlag, true);


        return true;
    }

    private bool HandleFirst(CellContext ctx, Regex rx, string input, bool assignFlag, bool remove)
    {
        Regex effectiveRx = rx;
        var match = effectiveRx.Match(input);
        if (!match.Success && _pattern.Contains("\\b"))
        {
            try
            {
                var altPattern = _pattern.Replace("\\b", string.Empty);
                var altWrapped = @"(?<!\p{L})(?:" + altPattern + @")(?!\p{L})";
                var altRx = new Regex(altWrapped, rx.Options);
                match = altRx.Match(input);
                if (match.Success)
                {
                    effectiveRx = altRx;
                }
            }
            catch
            {
            }
        }

        if (!match.Success)
        {
            ActionHelpers.WriteListOutput(ctx.PropertyBag, base.output, input, null, false, true);
            return false;
        }

        var result = GetMatchResult(match, effectiveRx);
        if (remove)
        {
            var cleaned = input.Substring(0, match.Index) + " " + input.Substring(match.Index + match.Length);
            cleaned = Regex.Replace(cleaned, "\\s+", " ").Trim();
            ActionHelpers.WriteListOutput(ctx.PropertyBag, base.output, cleaned, new List<string> { result },  assignFlag, true);
            return true;
        }

        ActionHelpers.WriteListOutput(ctx.PropertyBag, base.output, input, new List<string> { result }, assignFlag, true);

        return true;
    }
}
