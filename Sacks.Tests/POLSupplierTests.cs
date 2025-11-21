using ParsingEngine;
using System.Text.Json;
using Xunit;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;

public class POLSupplierTests
{
    private ParserEngine BuildPOLEngine()
    {
        // Load the supplier-formats.json and extract POL supplier config
        var supplierFormatsPath = @"c:\Users\avist\source\repos\GitHubLocal\Customers\Sacks-New\SacksApp\Configuration\supplier-formats.json";
        var jsonText = File.ReadAllText(supplierFormatsPath);
        var supplierDoc = JsonDocument.Parse(jsonText);
        var suppliersArray = supplierDoc.RootElement.GetProperty("suppliers");

        // Find POL supplier
        var polSupplier = suppliersArray.EnumerateArray()
            .FirstOrDefault(s => s.GetProperty("name").GetString() == "POL");

        if (polSupplier.ValueKind == JsonValueKind.Undefined)
        {
            throw new InvalidOperationException("POL supplier not found in supplier-formats.json!");
        }

        var parserConfigElement = polSupplier.GetProperty("parserConfig");
        var parserConfigJson = parserConfigElement.GetRawText();
        var cfg = ParserConfigLoader.FromJson(parserConfigJson);
        return new ParserEngine(cfg);
    }

    [Fact]
    public void POL_Brand_Extraction_From_Column_A()
    {
        var engine = BuildPOLEngine();
        
        // Test simple split parsing: [Brand]:[Gender]|[Type]:[Ref]
        var row = new RowData(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["A"] = "BULGARI:WOMENS|GIFTSET:41899"
        });

        var bag = engine.Parse(row);
        
        // Should extract brand and ref correctly
        Assert.True(bag.Values.ContainsKey("Product.Brand"));
        Assert.Equal("BULGARI", bag.Values["Product.Brand"]);
        Assert.Equal("41899", bag.Values["Offer.Ref"]);
        // TempGenderOrType should contain "WOMENS|GIFTSET"
    }

    [Fact]
    public void POL_Column_A_Simple_Split_Test()
    {
        var engine = BuildPOLEngine();
        
        // Test simple colon splitting
        var row = new RowData(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["A"] = "CHANEL:MENS|TESTER:T123"
        });

        var bag = engine.Parse(row);
        
        // Should extract brand and ref from split
        Assert.Equal("CHANEL", bag.Values["Product.Brand"]);
        Assert.Equal("T123", bag.Values["Offer.Ref"]);
    }

    [Fact]
    public void POL_Column_A_Invalid_Format_Should_Not_Crash()
    {
        var engine = BuildPOLEngine();
        
        // Test with invalid format - should not crash
        var row = new RowData(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["A"] = "INVALID_FORMAT_NO_SEPARATORS"
        });

        var bag = engine.Parse(row);
        
        // Should not crash, may not extract anything but that's ok
        // Just verify the parsing completes without exception
        Assert.NotNull(bag);
    }

    [Fact]
    public void POL_Simple_Columns_Assignment()
    {
        var engine = BuildPOLEngine();
        
        // Test simple direct assignments for C, D, E
        var row = new RowData(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["C"] = "7332204018990",  // EAN
            ["D"] = "50",             // Quantity  
            ["E"] = "678.00"          // Price
        });

        var bag = engine.Parse(row);
        
        // Check direct assignments
        Assert.Equal("7332204018990", bag.Values["Product.EAN"]);
        Assert.Equal("50", bag.Values["Offer.Quantity"]);
        Assert.Equal("678.00", bag.Values["Offer.Price"]);
    }

    [Fact]
    public void POL_Price_Column_E_Is_Clean_Decimal()
    {
        var engine = BuildPOLEngine();
        
        // Test that price column E receives clean decimal values (not "$678.00")
        var testPrices = new[] { "678.00", "33.50", "125.75", "1234.99" };
        
        foreach (var price in testPrices)
        {
            var row = new RowData(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["E"] = price
            });

            var bag = engine.Parse(row);
            Assert.Equal(price, bag.Values["Offer.Price"]);
        }
    }

    [Fact]
    public void POL_Complete_Row_Test()
    {
        var engine = BuildPOLEngine();
        
        // Test a complete row with the simpler split approach
        var row = new RowData(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["A"] = "BULGARI:WOMENS|GIFTSET:41899",
            ["B"] = "SPLENDIDA PATCHOULI TENTATION GIFTSET, 100ML / 3.4OZ",
            ["C"] = "7332204018990",
            ["D"] = "50",
            ["E"] = "678.00"
        });

        var bag = engine.Parse(row);
        
        // Verify basic assignments work
        Assert.Equal("BULGARI", bag.Values["Product.Brand"]);
        Assert.Equal("41899", bag.Values["Offer.Ref"]);
        Assert.Equal("SPLENDIDA PATCHOULI TENTATION GIFTSET, 100ML / 3.4OZ", bag.Values["Offer.Description"]);
        Assert.Equal("7332204018990", bag.Values["Product.EAN"]);
        Assert.Equal("50", bag.Values["Offer.Quantity"]);
        Assert.Equal("678.00", bag.Values["Offer.Price"]);
    }
}
