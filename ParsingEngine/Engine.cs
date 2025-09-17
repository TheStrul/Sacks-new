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
        var factory = new RuleFactory(config);
        _rulesByColumn = config.Columns
            .ToDictionary(c => c.Column,
                          c => c.Rules.Select(r => factory.Create(r)).OrderBy(r => r.Priority).ToList(),
                          StringComparer.OrdinalIgnoreCase);
    }

    public PropertyBag Parse(RowData row)
    {
        ArgumentNullException.ThrowIfNull(row);
        var bag = new PropertyBag { PreferFirstAssignment = _config.Settings.PreferFirstAssignment };

        foreach (var (column, raw) in row.Cells)
        {
            if (!_rulesByColumn.TryGetValue(column, out var rules)) continue;
            var ctx = new CellContext(column, raw, new CultureInfo(_config.Settings.DefaultCulture ?? "en-US"), new Dictionary<string, object?>());

            foreach (var rule in rules)
            {
                var result = rule.Execute(ctx);
                foreach (var a in result.Assignments)
                    bag.Set(a.Property, a.Value, a.SourceRuleId);

                if (_config.Settings.StopOnFirstMatchPerColumn && result.Matched)
                    break;
            }
        }
        return bag;
    }
}
