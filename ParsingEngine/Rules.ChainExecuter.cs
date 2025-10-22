namespace ParsingEngine;

public sealed class ChainExecuter : IRule
{
    private readonly List<IChainAction> _actions;
    private readonly Dictionary<string, Dictionary<string, string>> _lookups;
    private readonly RuleConfig _rc;

    public ChainExecuter(RuleConfig rc, ParserConfig config)
    {
        ArgumentNullException.ThrowIfNull(rc);
        ArgumentNullException.ThrowIfNull(config);
        _lookups = config.Lookups;
        _rc = rc;
        _actions = new();
        foreach (var s in rc.Actions ?? new())
            _actions.Add(ActionsFactory.Create(s, _lookups));
    }

    /// <summary>
    /// Execute the chain of actions against the provided context.
    /// </summary>
    public void Execute(CellContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        // Initialize Text variable with raw input
        ctx.PropertyBag.SetVariable("Text", ctx.Raw ?? string.Empty);

        // Execute actions
        foreach (var action in _actions)
        {
            action.Execute(ctx);
        }
    }
}
