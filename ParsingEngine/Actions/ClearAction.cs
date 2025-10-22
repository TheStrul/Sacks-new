namespace ParsingEngine;

/// <summary>
/// ClearAction: assigns an empty string to the target output when condition holds.
/// Parameters: none.
/// </summary>
public sealed class ClearAction : BaseAction
{
    public override string Op => "clear";

    public ClearAction(string fromKey, string toKey, bool assign, string? condition)
        : base(fromKey, toKey, assign, condition)
    {
    }

    public override bool Execute(CellContext ctx)
    {
        if (!base.Execute(ctx)) return false;
        if (assign)
            ctx.PropertyBag.SetAssign(output, string.Empty);
        else
            ctx.PropertyBag.SetVariable(output, string.Empty);
        return true;
    }
}
