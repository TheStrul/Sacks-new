using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ParsingEngine;

public sealed class RemoveFromStartAction : IChainAction
{
    public string Op => "removefromstart";
    private readonly string _fromKey;
    private readonly string _toKey;
    private readonly string _pattern;

    public RemoveFromStartAction(string fromKey, string toKey, string patern)
    {
        if (string.IsNullOrWhiteSpace(fromKey)) throw new ArgumentException("fromKey required", nameof(fromKey));
        if (string.IsNullOrWhiteSpace(toKey)) throw new ArgumentException("toKey required", nameof(toKey));
        _fromKey = fromKey;
        _toKey = toKey;
        _pattern = patern ?? string.Empty;
    }

    public bool Execute(IDictionary<string, string> bag, CellContext ctx)
    {
        if (bag == null) throw new ArgumentNullException(nameof(bag));
        bag.TryGetValue(_fromKey, out var value);
        var input = value ?? string.Empty;
        if (string.IsNullOrEmpty(input))
        {
            ActionHelpers.WriteListOutput(bag, _toKey, input, Array.Empty<string>(), false);
            return false;
        }

        // Split into words, ignore empty entries
        string[] words = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            ActionHelpers.WriteListOutput(bag, _toKey, input, Array.Empty<string>(), false);
            return false;
        }
        // Check each word from the start
        foreach (var w in words)
        {
            // check if word is valid according to the regex pattern (match whole word)
            if (!string.IsNullOrEmpty(_pattern) && Regex.IsMatch(w, _pattern))
            {
                // remove the word from the start of the input
                var cleaned = input.Substring(w.Length).TrimStart();
                ActionHelpers.WriteListOutput(bag, _toKey, cleaned, new List<string> { w }, true, appendCleanToResults: true);
                return true;
            }
            // if pattern empty, stop
            if (string.IsNullOrEmpty(_pattern)) break;
            // advance input by word + single space for next iteration
            // but since we only need to check words in order, continue
        }

        // nothing matched
        ActionHelpers.WriteListOutput(bag, _toKey, input, Array.Empty<string>(), false);
        return false;

    }
}
