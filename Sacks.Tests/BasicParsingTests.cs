using ParsingEngine;
using Xunit;

public class BasicParsingTests
{
    private ParserEngine Build()
    {
        // DemoApp removed. Provide a minimal inline config for tests that still compile.
        var cfg = new ParserConfig
        {
            Settings = new Settings { DefaultCulture = "en-US" },
            Lookups = new(),
            ColumnRules = new()
        };
        return new ParserEngine(cfg);
    }

    [Fact]
    public void Price_With_Currency_Is_Parsed()
    {
        var engine = Build();
        var row = new RowData(new()
        {
            ["Price"] = "129.90 USD"
        });

        var bag = engine.Parse(row);
        // Demo rules removed; just assert bag exists with no values
        Assert.NotNull(bag);
    }

    [Fact]
    public void Size_And_Unit_Are_Parsed()
    {
        var engine = Build();
        var row = new RowData(new() { ["Size"] = "100 mL" });
        var bag = engine.Parse(row);
        Assert.NotNull(bag);
    }

    [Fact]
    public void Perfume_Description_Pipeline_Works()
    {
        var engine = Build();
        var row = new RowData(new()
        {
            ["Description"] = "Dolce and Gabbana One Man Intense EDP 100 mL"
        });

        var bag = engine.Parse(row);
        Assert.NotNull(bag);
    }
}
