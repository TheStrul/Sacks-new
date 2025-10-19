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

    public override bool Execute(IDictionary<string, string> bag, CellContext ctx)
    {
        if (!base.Execute(bag, ctx)) return false;
        if (assign)
            bag[$"assign:{output}"] = string.Empty;
        else
            bag[output] = string.Empty;
        return true;
    }
}
