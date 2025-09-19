using System;
using System.Collections.Generic;
using System.Linq;

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
    /// Excute the chain of actions against the provided context.
    /// </summary>
    /// <param name="ctx"></param>
    /// <returns></returns>
    public RuleExecutionResult Execute(CellContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        // prepare working bag
        var bag = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Text"] = ctx.Raw ?? string.Empty
        };

        // seed static assigns if present
        if (_rc.Assign != null)
        {
            foreach (var kv in _rc.Assign)
            {
                if (string.IsNullOrWhiteSpace(kv.Key)) continue;
                bag[$"assign:{kv.Key}"] = kv.Value ?? string.Empty;
            }
        }

        // execute actions
        foreach (var action in _actions)
        {
            try
            {
                var ok = action.Execute(bag, ctx);

                // optional tracing
                if (_rc.Trace && ctx.Ambient.TryGetValue("PropertyBag", out var bagObj) && bagObj is PropertyBag pb)
                {
                    // Minimal trace: record which action ran and success
                    pb.Trace.Add($"Action {action.Op}: success={ok}");
                }
            }
            catch (Exception ex)
            {
                if (_rc.Trace && ctx.Ambient.TryGetValue("PropertyBag", out var bagObj) && bagObj is PropertyBag pb)
                {
                    pb.Trace.Add($"Action {action.Op} threw: {ex.Message}");
                }
            }
        }

        // Collect assignments from bag entries with 'assign:' prefix
        var assigns = bag
            .Where(kv => kv.Key.StartsWith("assign:", StringComparison.OrdinalIgnoreCase))
            .Select(kv => new Assignment(kv.Key["assign:".Length..], kv.Value, "Actions"))
            .ToList();

        return new(assigns.Any(), assigns);
    }
}
