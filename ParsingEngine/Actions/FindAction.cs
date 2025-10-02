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

    public override bool Execute(IDictionary<string, string> bag, CellContext ctx)
    {
        if (base.Execute(bag, ctx) == false) return false; // basic checks

        bag.TryGetValue(base.input, out var value);
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
                    "all" => HandleAllLookup(bag, ctx, input, ignoreCase, remove, base.assign),
                    "last" => HandleLastLookup(bag, ctx, input, ignoreCase, remove, base.assign),
                    _ => HandleFirstLookup(bag, ctx, input, ignoreCase, remove, base.assign),
                };
            }

            // Dynamic pattern from bag/ambient PropertyBag via PatternKey
            if (!string.IsNullOrWhiteSpace(_patternKey))
            {
                string? dyn = null;
                if (!bag.TryGetValue(_patternKey!, out dyn) || string.IsNullOrWhiteSpace(dyn))
                {
                    // Try ambient PropertyBag values
                    if (ctx.Ambient != null && ctx.Ambient.TryGetValue("PropertyBag", out var obj) && obj is PropertyBag pb)
                    {
                        pb.Values.TryGetValue(_patternKey!, out var dynObj);
                        dyn = dynObj?.ToString();
                    }
                }

                if (string.IsNullOrWhiteSpace(dyn) || string.IsNullOrEmpty(input))
                {
                    ActionHelpers.WriteListOutput(bag, base.output, input, null, false, true);
                    return false;
                }

                var pat = $"(?<!\\p{{L}})" + Regex.Escape(dyn!) + $"(?!\\p{{L}})";
                var rx = new Regex(pat, ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);

                return mode.ToLowerInvariant() switch
                {
                    "all" => remove ? RemoveAll(bag, rx, input, base.assign) : FindAll(bag, rx, input, base.assign),
                    "last" => remove ? RemoveLast(bag, rx, input, base.assign) : FindLast(bag, rx, input, base.assign),
                    _ => remove ? RemoveFirst(bag, rx, input, base.assign) : FindFirst(bag, rx, input, base.assign),
                };
            }

            // Static regex pattern
            if (string.IsNullOrEmpty(_pattern) || string.IsNullOrEmpty(input))
            {
                ActionHelpers.WriteListOutput(bag, base.output, input, null, false, true);
                return false;
            }

            var rxOpts = ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
            var rxStatic = new Regex(_pattern, rxOpts);

            return mode.ToLowerInvariant() switch
            {
                "all" => remove ? RemoveAll(bag, rxStatic, input, base.assign) : FindAll(bag, rxStatic, input, base.assign),
                "last" => remove ? RemoveLast(bag, rxStatic, input, base.assign) : FindLast(bag, rxStatic, input, base.assign),
                _ => remove ? RemoveFirst(bag, rxStatic, input, base.assign) : FindFirst(bag, rxStatic, input, base.assign),
            };
        }
        catch
        {
            ActionHelpers.WriteListOutput(bag, base.output, input, null, false, true);
            return false;
        }
    }

    private void AddTrace(CellContext ctx, string text)
    {
        try
        {
            if (ctx.Ambient != null && ctx.Ambient.TryGetValue("PropertyBag", out var obj) && obj is PropertyBag pb)
            {
                pb.Trace.Add(text);
            }
        }
        catch
        {
            // ignore tracing failures
        }
    }

    // Publicly requested small methods (private) delegating to core handlers
    private bool FindAll(IDictionary<string, string> bag, Regex rx, string input, bool assignFlag) => HandleAll(bag, rx, input, assignFlag, remove: false);
    private bool RemoveAll(IDictionary<string, string> bag, Regex rx, string input, bool assignFlag) => HandleAll(bag, rx, input, assignFlag, remove: true);
    private bool FindLast(IDictionary<string, string> bag, Regex rx, string input, bool assignFlag) => HandleLast(bag, rx, input, assignFlag, remove: false);
    private bool RemoveLast(IDictionary<string, string> bag, Regex rx, string input, bool assignFlag) => HandleLast(bag, rx, input, assignFlag, remove: true);
    private bool FindFirst(IDictionary<string, string> bag, Regex rx, string input, bool assignFlag) => HandleFirst(bag, rx, input, assignFlag, remove: false);
    private bool RemoveFirst(IDictionary<string, string> bag, Regex rx, string input, bool assignFlag) => HandleFirst(bag, rx, input, assignFlag, remove: true);

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

    private bool HandleAllLookup(IDictionary<string, string> bag, CellContext ctx, string input, bool ignoreCase, bool remove, bool assignFlag)
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
            ActionHelpers.WriteListOutput(bag, base.output, input, null, false, true);
            return false;
        }

        if (remove)
        {
            // remove all matches of each key in lookup order
            var cleaned = input;
            foreach (var kv in OrderedLookupEntries(ignoreCase))
            {
                var key = kv.Key ?? string.Empty;
                try
                {
                    var pat = $"(?<!\\p{{L}})" + Regex.Escape(key) + $"(?!\\p{{L}})";
                    cleaned = Regex.Replace(cleaned, pat, string.Empty, ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
                }
                catch { }
            }
            cleaned = Regex.Replace(cleaned, "\\s+", " ").Trim();
            ActionHelpers.WriteListOutput(bag, base.output, cleaned, results, assignFlag, false);
        }
        else
        {
            ActionHelpers.WriteListOutput(bag, base.output, input, results, assignFlag, false);
        }

        return true;
    }

    private bool HandleFirstLookup(IDictionary<string, string> bag, CellContext ctx, string input, bool ignoreCase, bool remove, bool assignFlag)
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
                        var cleaned = input.Remove(match.Index, match.Length);
                        cleaned = Regex.Replace(cleaned, "\\s+", " ").Trim();
                        ActionHelpers.WriteListOutput(bag, base.output, cleaned, new List<string> { mapped }, assignFlag, true);
                        return true;
                    }

                    ActionHelpers.WriteListOutput(bag, base.output, input, new List<string> { mapped }, assignFlag, true);
                    return true;
                }
            }
            catch { }
        }

        ActionHelpers.WriteListOutput(bag, base.output, input, null, false, true);
        return false;
    }

    private bool HandleLastLookup(IDictionary<string, string> bag, CellContext ctx, string input, bool ignoreCase, bool remove, bool assignFlag)
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
            ActionHelpers.WriteListOutput(bag, base.output, input, null, false, true);
            return false;
        }

        if (remove)
        {
            var cleaned = input.Remove(lastMatch.Index, lastMatch.Length);
            cleaned = Regex.Replace(cleaned, "\\s+", " ").Trim();
            ActionHelpers.WriteListOutput(bag, base.output, cleaned, new List<string> { lastMapped! }, assignFlag, true);
            return true;
        }

        ActionHelpers.WriteListOutput(bag, base.output, input, new List<string> { lastMapped! }, assignFlag, true);
        return true;
    }

    private bool HandleAll(IDictionary<string, string> bag, Regex rx, string input, bool assignFlag, bool remove)
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
            ActionHelpers.WriteListOutput(bag, base.output, input, null, false, true);
            return false;
        }

        // Use group values when available (e.g., 'size') so results list contains clean values while the regex
        // match may include surrounding separators for removal.
        var results = matches.Select(m => GetMatchResult(m, effectiveRx)).ToList();

        if (remove)
        {
            var cleaned = effectiveRx.Replace(input, string.Empty);
            cleaned = Regex.Replace(cleaned, "\\s+", " ").Trim();
            ActionHelpers.WriteListOutput(bag, base.output, cleaned, results, assignFlag, false);
        }
        else
        {
            ActionHelpers.WriteListOutput(bag, base.output, input, results, assignFlag, false);
        }

        return true;
    }

    private bool HandleLast(IDictionary<string, string> bag, Regex rx, string input, bool assignFlag, bool remove)
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
            ActionHelpers.WriteListOutput(bag, base.output, input, null, false, true);
            return false;
        }

        var m = matches[^1];
        var result = GetMatchResult(m, effectiveRx);
        if (remove)
        {
            var cleaned = input.Remove(m.Index, m.Length);
            cleaned = Regex.Replace(cleaned, "\\s+", " ").Trim();
            ActionHelpers.WriteListOutput(bag, base.output, cleaned, new List<string> { result }, assignFlag, true);
            return true;
        }

        ActionHelpers.WriteListOutput(bag, base.output, input, new List<string> { result }, assignFlag, true);


        return true;
    }

    private bool HandleFirst(IDictionary<string, string> bag, Regex rx, string input, bool assignFlag, bool remove)
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
            ActionHelpers.WriteListOutput(bag, base.output, input, null, false, true);
            return false;
        }

        var result = GetMatchResult(match, effectiveRx);
        if (remove)
        {
            var cleaned = input.Remove(match.Index, match.Length);
            cleaned = Regex.Replace(cleaned, "\\s+", " ").Trim();
            ActionHelpers.WriteListOutput(bag, base.output, cleaned, new List<string> { result },  assignFlag, true);
            return true;
        }

        ActionHelpers.WriteListOutput(bag, base.output, input, new List<string> { result }, assignFlag, true);

        return true;
    }
}
