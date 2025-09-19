using System;
using System.Collections.Generic;
using System.Linq;

namespace ParsingEngine;

/// <summary>
/// Minimal runtime contract for chain actions.
/// </summary>
public interface IChainAction
{
    /// <summary>
    /// Operation name (lower-case recommended).
    /// </summary>
    string Op { get; }

    /// <summary>
    /// Execute the action against the shared bag. Return true if action succeeded.
    /// </summary>
    bool Execute(IDictionary<string, string> bag, CellContext ctx);
}

/// <summary>
/// Simple assign action: reads a source key from the bag and writes it to the target key.
/// Always succeeds (writes empty string when source missing).
/// </summary>
public sealed class SimpleAssignAction : IChainAction
{
    public string Op => "assign";
    private readonly string _fromKey;
    private readonly string _toKey;
    private readonly string[] _options;
    private readonly IDictionary<string, string> _parameters;

    public SimpleAssignAction(string fromKey, string toKey, IEnumerable<string>? options = null, IDictionary<string, string>? parameters = null)
    {
        if (string.IsNullOrWhiteSpace(fromKey)) throw new ArgumentException("fromKey required", nameof(fromKey));
        if (string.IsNullOrWhiteSpace(toKey)) throw new ArgumentException("toKey required", nameof(toKey));
        _fromKey = fromKey;
        _toKey = toKey;
        _options = options?.ToArray() ?? Array.Empty<string>();
        _parameters = parameters != null
            ? new Dictionary<string, string>(parameters, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    public bool Execute(IDictionary<string, string> bag, CellContext ctx)
    {
        if (bag == null) throw new ArgumentNullException(nameof(bag));
        bag.TryGetValue(_fromKey, out var value);
        var val = value ?? string.Empty;
        // write standardized list output where result[3] contains the assigned value
        ActionHelpers.WriteListOutput(bag, _toKey, val, new List<string> { val }, true);
        return true;
    }
}
