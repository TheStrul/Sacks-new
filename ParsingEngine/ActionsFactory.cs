using System;
using System.Collections.Generic;
using System.Linq;

namespace ParsingEngine;

/// <summary>
/// Compatibility factory that creates parsing steps or chain actions from config objects.
/// </summary>
public static class ActionsFactory
{


    // New: create chain action from ActionConfig
    public static IChainAction Create(ActionConfig s, Dictionary<string, Dictionary<string, string>> lookups)
    {
        if (s == null) throw new ArgumentNullException(nameof(s));
        if (lookups == null) throw new ArgumentNullException(nameof(lookups));

        var op = (s.Op ?? string.Empty).ToLowerInvariant();
        var input = s.Input ?? string.Empty;
        var output = s.Output ?? string.Empty;
        var pattern = s.Pattern ?? string.Empty;
        var options = s.Options ?? new List<string>();

        return op switch
        {
            "assign" => new SimpleAssignAction(input, output, options),
            "find" => new FindAction(input, output, pattern, options),
            "split" => new SplitAction(input, output, string.IsNullOrEmpty(pattern) ? ":" : pattern),
            "removefromstart" => new RemoveFromStartAction(input, output, pattern),
            _ => new NoOpChainAction(input, output),
        };
    }

    private sealed class NoOpChainAction : IChainAction
    {
        private readonly string _from;
        private readonly string _to;
        public NoOpChainAction(string from, string to)
        {
            _from = from;
            _to = to;
        }
        public string Op => "noop";
        public bool Execute(IDictionary<string, string> bag, CellContext ctx)
        {
            if (bag == null) throw new ArgumentNullException(nameof(bag));
            bag.TryGetValue(_from, out var value);
            bag[_to] = value ?? string.Empty;
            return true;
        }
    }
}
