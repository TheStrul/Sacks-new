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

    public SimpleAssignAction(string fromKey, string toKey)
    {
        _fromKey = fromKey;
        _toKey = toKey;
    }

    public bool Execute(IDictionary<string, string> bag, CellContext ctx)
    {
        if (bag == null) throw new ArgumentNullException(nameof(bag));
        bag.TryGetValue(_fromKey, out var value);

        // No need to use ActionHelpers to write one value 
        bag[$"assign:{_toKey}"] = value ?? string.Empty;
        return true;
    }
}
