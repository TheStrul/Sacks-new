using ParsingEngine;
using Xunit;

public class BasicParsingTests
{
    private ParserEngine Build()
    {
        var cfg = ParserConfigLoader.FromJsonFile("DemoApp/rules/parsing.rules.en-US.json");
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
        Assert.Equal(129.90m, (decimal)bag.Values["Price"]);
        Assert.Equal("USD", (string)bag.Values["Currency"]);
    }

    [Fact]
    public void Size_And_Unit_Are_Parsed()
    {
        var engine = Build();
        var row = new RowData(new() { ["Size"] = "100 mL" });
        var bag = engine.Parse(row);
        Assert.Equal(100m, decimal.Parse((string)bag.Values["SizeValue"]));
        Assert.Equal("ml", (string)bag.Values["SizeUnit"]);
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
        Assert.Equal("Dolce & Gabbana", (string)bag.Values["Brand"]);
        Assert.Equal("One Man Intense", (string)bag.Values["Series"]);
        Assert.Equal("Eau de Parfum", (string)bag.Values["Concentration"]);
        Assert.Equal("ml", (string)bag.Values["SizeUnit"]);
        Assert.Equal("100", (string)bag.Values["SizeValue"]);
    }
}
