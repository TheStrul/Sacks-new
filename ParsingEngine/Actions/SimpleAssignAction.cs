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
public sealed class SimpleAssignAction : BaseAction
{
    public override string Op => "assign";

    public SimpleAssignAction(string fromKey, string toKey, bool assign = true, string? condition = null) : base(fromKey, toKey,assign,condition)
    {
    }

    public override bool Execute(IDictionary<string, string> bag, CellContext ctx)
    {
        if (base.Execute(bag, ctx) == false) return false;
        if (bag.TryGetValue(input, out var value))
        {
            bag[$"assign:{output}"] = value ?? string.Empty;
            return true;
        }
        return false;
    }
}
