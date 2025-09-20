using System;
using System.Collections.Generic;
using System.Linq;

namespace ParsingEngine;

/// <summary>
/// Compatibility factory that creates parsing steps or chain actions from config objects.
/// </summary>
public static class ActionsFactory
{


    // create chain action from ActionConfig
    public static IChainAction Create(ActionConfig s, Dictionary<string, Dictionary<string, string>> lookups)
    {
        if (s == null) throw new ArgumentNullException(nameof(s));
        if (lookups == null) throw new ArgumentNullException(nameof(lookups));

        var op = (s.Op ?? string.Empty).ToLowerInvariant();
        var input = s.Input ?? string.Empty;
        var output = s.Output ?? string.Empty;
        var patameter = s.Parameters ?? new Dictionary<string,string>();

        IChainAction? ret;
        switch (op)
        {
            case "assign":
                ret = new SimpleAssignAction(input, output);
                break;
            case "conditional":
                // ensure we have the required parameters
                if (!patameter.ContainsKey("condition"))
                    throw new ArgumentException("Missing required parameter 'condition' for conditional action");
                string condition = patameter["condition"];
                bool assign = patameter.ContainsKey("assign") && patameter["assign"].Equals("true",StringComparison.OrdinalIgnoreCase);
                ret = new ConditionalAssignAction(input, output,  condition, assign);
                break;
            case "find":
                string pattern = patameter.ContainsKey("pattern") ? patameter["pattern"] : string.Empty;
                string[] options = patameter.ContainsKey("options") ? patameter["options"].Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries) : Array.Empty<string>();
                ret = new FindAction(input, output, pattern, options.ToList());
                break;
            case "split":
                // delimiter parameter expected in Parameters["delimiter"], default to ':'
                var delimiter = patameter.ContainsKey("delimiter") ? patameter["delimiter"] : ":";
                ret = new SplitAction(input, output, delimiter);
                break;

            default:
                ret = new NoOpChainAction(input, output);
                break;
        }
        return ret;
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
