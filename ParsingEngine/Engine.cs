using System.Globalization;

namespace ParsingEngine;

public sealed class ParserEngine
{
    private readonly ParserConfig _config;

    public ParserEngine(ParserConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        _config = config;
    }

    public PropertyBag Parse(RowData row)
    {
        return Parse(row, initialAssignments: null);
    }

    /// <summary>
    /// Parse a row with optional initial assignments pre-seeded into the PropertyBag before rules execute.
    /// Use this to provide defaults (e.g., subtitle-derived values) that should win when PreferFirstAssignment is enabled.
    /// </summary>
    public PropertyBag Parse(RowData row, IDictionary<string, object?>? initialAssignments)
    {
        ArgumentNullException.ThrowIfNull(row);
        var bag = new PropertyBag ();

        // Pre-seed initial assignments (appear as first-writer if PreferFirstAssignment is true)
        if (initialAssignments != null)
        {
            foreach (var kv in initialAssignments)
            {
                if (string.IsNullOrWhiteSpace(kv.Key)) continue;
                bag.SetAssign(kv.Key, kv.Value);
            }
        }

        // Iterate through rules in defined order, not cells
        foreach (var ruleConfig in _config.ColumnRules)
        {
            var column = ruleConfig.Column;

            // Look up the cell value for this rule's column
            if (!row.Cells.TryGetValue(column, out var raw))
                continue; // Skip if column not present in row

            var ctx = new CellContext(column, raw, new CultureInfo(_config.Settings.DefaultCulture), bag);

            var exe = new ChainExecuter(ruleConfig, _config);
            
            exe.Execute(ctx);
        }
        return bag;
    }
}

internal static class EngineHelpers
{
    internal static List<IRule> BuildRulesForColumn(RuleConfig c, ParserConfig config)
    {
        var rules = new List<IRule>();

        rules.Add(new ChainExecuter(c, config));

        return rules;
    }
}
