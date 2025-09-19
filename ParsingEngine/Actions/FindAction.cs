using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ParsingEngine;

public sealed class FindAction : IChainAction
{
    public string Op => "find";
    private readonly string _fromKey;
    private readonly string _toKey;
    private readonly string _pattern;
    private readonly string[] _options;
    private readonly IDictionary<string, string> _parameters;

    public FindAction(string fromKey, string toKey, string patern, IEnumerable<string>? options = null, IDictionary<string,string>? parameters = null)
    {
        if (string.IsNullOrWhiteSpace(fromKey)) throw new ArgumentException("fromKey required", nameof(fromKey));
        if (string.IsNullOrWhiteSpace(toKey)) throw new ArgumentException("toKey required", nameof(toKey));
        _fromKey = fromKey;
        _toKey = toKey;
        _pattern = patern ?? string.Empty;
        _options = options?.ToArray() ?? Array.Empty<string>();
        _parameters = parameters != null
            ? new Dictionary<string, string>(parameters, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    public bool Execute(IDictionary<string, string> bag, CellContext ctx)
    {
        if (bag == null) throw new ArgumentNullException(nameof(bag));
        bag.TryGetValue(_fromKey, out var value);
        var input = value ?? string.Empty;
        if (string.IsNullOrEmpty(_pattern) || string.IsNullOrEmpty(input))
        {
            ActionHelpers.WriteListOutput(bag, _toKey, input, Array.Empty<string>(), false);
            return false;
        }

        // Build options string compatible with ParsingHelpers.ToOptions (for regex flags)
        var optsForRegex = string.Join(" ", _options);
        try
        {
            var rx = new Regex(_pattern);

            // Determine mode (First/Last/All)
            var mode = _options.FirstOrDefault(o => string.Equals(o, "all", StringComparison.OrdinalIgnoreCase)
                                                  || string.Equals(o, "last", StringComparison.OrdinalIgnoreCase)
                                                  || string.Equals(o, "first", StringComparison.OrdinalIgnoreCase))
                       ?? "first";

            var remove = _options.Any(o => string.Equals(o, "remove", StringComparison.OrdinalIgnoreCase));

            if (string.Equals(mode, "all", StringComparison.OrdinalIgnoreCase))
            {
                var matches = rx.Matches(input).Cast<Match>().ToArray();
                if (matches.Length == 0)
                {
                    ActionHelpers.WriteListOutput(bag, _toKey, input, Array.Empty<string>(), false);
                    return false;
                }

                var results = matches.Select(m => m.Value).ToList();

                if (remove)
                {
                    // remove all matches
                    var cleaned = rx.Replace(input, string.Empty);
                    cleaned = Regex.Replace(cleaned, "\\s+", " ").Trim();
                    ActionHelpers.WriteListOutput(bag, _toKey, cleaned, results, true, appendCleanToResults: true);
                    return true;
                }

                // write matches only
                ActionHelpers.WriteListOutput(bag, _toKey, input, results, true, appendCleanToResults: false);

                // write named groups per match as sub-keys with index starting at 3
                for (int i = 0; i < matches.Length; i++)
                {
                    foreach (var name in rx.GetGroupNames())
                    {
                        if (int.TryParse(name, out _)) continue;
                        var gkey = $"{_toKey}[{i + 3}].{name}";
                        bag[gkey] = matches[i].Groups[name].Value;
                    }
                }

                return true;
            }

            if (string.Equals(mode, "last", StringComparison.OrdinalIgnoreCase))
            {
                var matches = rx.Matches(input).Cast<Match>().ToArray();
                if (matches.Length == 0)
                {
                    ActionHelpers.WriteListOutput(bag, _toKey, input, Array.Empty<string>(), false);
                    return false;
                }
                var m = matches[^1];
                if (remove)
                {
                    // remove only last match
                    var cleaned = input.Remove(m.Index, m.Length);
                    cleaned = Regex.Replace(cleaned, "\\s+", " ").Trim();
                    ActionHelpers.WriteListOutput(bag, _toKey, cleaned, new List<string> { m.Value }, true, appendCleanToResults: true);
                    return true;
                }

                ActionHelpers.WriteListOutput(bag, _toKey, input, new List<string> { m.Value }, true, appendCleanToResults: false);
                foreach (var name in rx.GetGroupNames())
                {
                    if (int.TryParse(name, out _)) continue;
                    var key = $"{_toKey}.3.{name}"; // single result will be at index 3
                    bag[key] = m.Groups[name].Value;
                }
                return true;
            }

            // Default: first
            var match = rx.Match(input);
            if (!match.Success)
            {
                ActionHelpers.WriteListOutput(bag, _toKey, input, Array.Empty<string>(), false);
                return false;
            }

            if (remove)
            {
                // remove first match
                var cleaned = input.Remove(match.Index, match.Length);
                cleaned = Regex.Replace(cleaned, "\\s+", " ").Trim();
                ActionHelpers.WriteListOutput(bag, _toKey, cleaned, new List<string> { match.Value }, true, appendCleanToResults: true);
                return true;
            }

            // No remove: return matched value
            ActionHelpers.WriteListOutput(bag, _toKey, input, new List<string> { match.Value }, true, appendCleanToResults: false);
            foreach (var name in rx.GetGroupNames())
            {
                if (int.TryParse(name, out _)) continue;
                var key = $"{_toKey}.3.{name}"; // first result placed at index 3
                bag[key] = match.Groups[name].Value;
            }

            return true;
        }
        catch
        {
            ActionHelpers.WriteListOutput(bag, _toKey, input, Array.Empty<string>(), false);
            return false;
        }
    }
}
