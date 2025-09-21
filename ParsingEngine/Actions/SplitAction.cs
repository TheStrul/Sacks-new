using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ParsingEngine;

/// <summary>
/// SplitAction: splits a source value by a delimiter and writes parts into a target collection
/// as Target[0], Target[1], ... and sets Target.Length and Target.Valid flags.
/// </summary>
public sealed class SplitAction : IChainAction
{
    public string Op => "split";
    private readonly string _fromKey;
    private readonly string _toKey;
    private readonly string _delimiter;

    public SplitAction(string fromKey, string toKey, string delimiter = ":")
    {
        _fromKey = fromKey;
        _toKey = toKey;
        _delimiter = delimiter ?? ":";
    }

    public bool Execute(IDictionary<string, string> bag, CellContext ctx)
    {
        if (bag == null) throw new ArgumentNullException(nameof(bag));
        bag.TryGetValue(_fromKey, out var value);
        var input = value ?? string.Empty;

        if (string.IsNullOrEmpty(input))
        {
            ActionHelpers.WriteListOutput(bag, _toKey, input, null, false);
            return false;
        }

        var parts = input.Split(new[] { _delimiter }, StringSplitOptions.None)
                         .Select(p => p.Trim())
                         .ToArray();

        ActionHelpers.WriteListOutput(bag, _toKey, input, parts, false);
        return true;
    }
}
