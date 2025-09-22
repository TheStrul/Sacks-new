using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ParsingEngine;

public sealed class FindAction : IChainAction
{
    public string Op => "find";
    private readonly string _fromKey;
    private readonly string _toKey;
    private readonly string _pattern;
    private readonly List<string> _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="FindAction"/> class with the specified range, pattern, and options.
    /// Supports regex patterns
    /// Options:
    /// "first" (default) - find first match
    /// "last" - find last match
    /// "all" - find all matches
    /// "remove" - remove matches from input and return the rest
    /// "ignorecase" - case insensitive matching
    /// "assign" - add 'assign' to the output key (to indicate this is an assign action)
    /// </summary>
    /// <param name="fromKey">The key name that holds the value (Input). Cannot be null.</param>
    /// <param name="toKey">The key array name to write the output . Cannot be null.</param>
    /// <param name="pattern">The pattern to match during the search. If null, an empty string is used.</param>
    /// <param name="options">A list of options that modify the search behavior. If null, an empty list is used.</param>
    public FindAction(string fromKey, string toKey, string pattern, List<string> options)
    {
        _fromKey = fromKey;
        _toKey = toKey;
        _pattern = pattern ?? string.Empty;
        _options = options ?? new List<string>();
    }

    public bool Execute(IDictionary<string, string> bag, CellContext ctx)
    {
        if (bag == null) throw new ArgumentNullException(nameof(bag));

        bag.TryGetValue(_fromKey, out var value);
        var input = value ?? string.Empty;

        // If no pattern or empty input -> write empty results and return false
        if (string.IsNullOrEmpty(_pattern) || string.IsNullOrEmpty(input))
        {
            ActionHelpers.WriteListOutput(bag, _toKey, input, null, false, true);
            return false;
        }

        var assignFlag = _options.Any(o => string.Equals(o, "assign", StringComparison.OrdinalIgnoreCase));
        var remove = _options.Any(o => string.Equals(o, "remove", StringComparison.OrdinalIgnoreCase));
        var ignoreCase = _options.Any(o => string.Equals(o, "ignorecase", StringComparison.OrdinalIgnoreCase));

        var mode = _options.FirstOrDefault(o => string.Equals(o, "all", StringComparison.OrdinalIgnoreCase)
                                              || string.Equals(o, "last", StringComparison.OrdinalIgnoreCase)
                                              || string.Equals(o, "first", StringComparison.OrdinalIgnoreCase))
                   ?? "first";

        try
        {
            var rxOpts = ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
            var rx = new Regex(_pattern, rxOpts);

            return mode.ToLowerInvariant() switch
            {
                "all" => remove ? RemoveAll(bag, rx, input, assignFlag) : FindAll(bag, rx, input, assignFlag),
                "last" => remove ? RemoveLast(bag, rx, input, assignFlag) : FindLast(bag, rx, input, assignFlag),
                _ => remove ? RemoveFirst(bag, rx, input, assignFlag) : FindFirst(bag, rx, input, assignFlag),
            };
        }
        catch
        {
            ActionHelpers.WriteListOutput(bag, _toKey, input, null, false, true);
            return false;
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

    private bool HandleAll(IDictionary<string, string> bag, Regex rx, string input, bool assignFlag, bool remove)
    {
        var matches = rx.Matches(input).Cast<Match>().ToArray();
        if (matches.Length == 0)
        {
            ActionHelpers.WriteListOutput(bag, _toKey, input, null, false, true);
            return false;
        }

        // Use group values when available (e.g., 'size') so results list contains clean values while the regex
        // match may include surrounding separators for removal.
        var results = matches.Select(m => GetMatchResult(m, rx)).ToList();

        if (remove)
        {
            var cleaned = rx.Replace(input, string.Empty);
            cleaned = Regex.Replace(cleaned, "\\s+", " ").Trim();
            ActionHelpers.WriteListOutput(bag, _toKey, cleaned, results, assignFlag,false);
        }
        else
        {
            ActionHelpers.WriteListOutput(bag, _toKey, input, results, assignFlag, false);
        }

        return true;
    }

    private bool HandleLast(IDictionary<string, string> bag, Regex rx, string input, bool assignFlag, bool remove)
    {
        var matches = rx.Matches(input).Cast<Match>().ToArray();
        if (matches.Length == 0)
        {
            ActionHelpers.WriteListOutput(bag, _toKey, input, null, false, true);
            return false;
        }

        var m = matches[^1];
        var result = GetMatchResult(m, rx);
        if (remove)
        {
            var cleaned = input.Remove(m.Index, m.Length);
            cleaned = Regex.Replace(cleaned, "\\s+", " ").Trim();
            ActionHelpers.WriteListOutput(bag, _toKey, cleaned, new List<string> { result }, assignFlag, true);
            return true;
        }

        ActionHelpers.WriteListOutput(bag, _toKey, input, new List<string> { result }, assignFlag, true);


        return true;
    }

    private bool HandleFirst(IDictionary<string, string> bag, Regex rx, string input, bool assignFlag, bool remove)
    {
        var match = rx.Match(input);
        if (!match.Success)
        {
            ActionHelpers.WriteListOutput(bag, _toKey, input, null, false, true);
            return false;
        }

        var result = GetMatchResult(match, rx);
        if (remove)
        {
            var cleaned = input.Remove(match.Index, match.Length);
            cleaned = Regex.Replace(cleaned, "\\s+", " ").Trim();
            ActionHelpers.WriteListOutput(bag, _toKey, cleaned, new List<string> { result },  assignFlag, true);
            return true;
        }

        ActionHelpers.WriteListOutput(bag, _toKey, input, new List<string> { result }, assignFlag, true);

        return true;
    }
}
