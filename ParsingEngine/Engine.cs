using System.Globalization;

namespace ParsingEngine;

public sealed class ParserEngine
{
    private readonly ParserConfig _config;
    private readonly Dictionary<string, List<IRule>> _rulesByColumn;

    public ParserEngine(ParserConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        _config = config;
    _rulesByColumn = config.ColumnRules
            .ToDictionary(
                c => c.Key,
        c => EngineHelpers.BuildRulesForColumn(c.Value, _config),
                StringComparer.OrdinalIgnoreCase);
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
        var bag = new PropertyBag { PreferFirstAssignment = _config.Settings.PreferFirstAssignment };

        // Pre-seed initial assignments (appear as first-writer if PreferFirstAssignment is true)
        if (initialAssignments != null)
        {
            foreach (var kv in initialAssignments)
            {
                if (string.IsNullOrWhiteSpace(kv.Key)) continue;
                bag.Set(kv.Key, kv.Value, "Seed");
            }
        }

        foreach (var (column, raw) in row.Cells)
        {
            if (!_rulesByColumn.TryGetValue(column, out var rules)) continue;
            var ambient = new Dictionary<string, object?>();
            ambient["PropertyBag"] = bag; // allow rules/steps to append trace
            var ctx = new CellContext(column, raw, new CultureInfo(_config.Settings.DefaultCulture ?? "en-US"), ambient);

            foreach (var rule in rules)
            {
                var result = rule.Execute(ctx);
                foreach (var a in result.Assignments)
                    bag.Set(a.Property, a.Value, a.Source);

                if (_config.Settings.StopOnFirstMatchPerColumn && result.Matched)
                    break;
            }
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
