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
    /// Execute the action against the PropertyBag variables. Return true if action succeeded.
    /// </summary>
    bool Execute(CellContext ctx);
}

/// <summary>
/// Simple assign action: reads a source key from variables and writes it to target (Assignes if assign=true, Variables otherwise).
/// </summary>
public sealed class SimpleAssignAction : BaseAction
{
    public override string Op => "assign";

    public SimpleAssignAction(string fromKey, string toKey, bool assign = true, string? condition = null) : base(fromKey, toKey, assign, condition)
    {
    }

    public override bool Execute(CellContext ctx)
    {
        if (base.Execute(ctx) == false) return false;
        if (ctx.PropertyBag.Variables.TryGetValue(input, out var value))
        {
            if (assign)
                ctx.PropertyBag.SetAssign(output, value ?? string.Empty);
            else
                ctx.PropertyBag.SetVariable(output, value ?? string.Empty);
            return true;
        }
        return false;
    }
}
