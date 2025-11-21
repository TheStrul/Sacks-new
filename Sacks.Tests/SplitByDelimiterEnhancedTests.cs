using ParsingEngine;
using System.Text.Json;
using Xunit;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;

namespace ParsingEngine.Tests
{
    public class SplitByDelimiterEnhancedTests
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
        public void POL_Enhanced_Normal_Case_3_Parts()
        {
            var engine = BuildPOLEngine();
            
            // Test normal case: exactly 3 parts
            var row = new RowData(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["A"] = "CHANEL:MENS|TESTER:T123"
            });

            var bag = engine.Parse(row);
            
            Console.WriteLine("Test: Normal case - 3 parts");
            Console.WriteLine("Final PropertyBag values:");
            foreach (var kvp in bag.Values)
            {
                Console.WriteLine($"  {kvp.Key} = '{kvp.Value}'");
            }
            
            // Should work normally under current POL config (strict split + conditional mapping)
            Assert.Equal("CHANEL", bag.Values["Product.Brand"]);
            Assert.Equal("T123", bag.Values["Offer.Ref"]);
            // Gender is mapped to single-letter code by ConditionalMapping
            Assert.Equal("M", bag.Values["Product.Gender"]);
            Assert.Equal("Tester", bag.Values["Product.Type"]);
        }

        [Fact]
        public void POL_Enhanced_Two_Parts_Non_Strict()
        {
            var engine = BuildPOLEngine();
            
            // Test with only 2 parts (non-strict should pad with empty)
            var row = new RowData(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["A"] = "CHANEL:MENS"
            });

            var bag = engine.Parse(row);
            
            Console.WriteLine("Test: 2 parts, non-strict mode");
            Console.WriteLine("Final PropertyBag values:");
            foreach (var kvp in bag.Values)
            {
                Console.WriteLine($"  {kvp.Key} = '{kvp.Value}'");
            }
            
            // With strict mode in config, invalid parts count leaves assignments empty
            Assert.Equal(string.Empty, bag.Values.GetValueOrDefault("Product.Brand", string.Empty));
            Assert.Equal(string.Empty, bag.Values.GetValueOrDefault("Offer.Ref", string.Empty));
        }

        [Fact]
        public void POL_Enhanced_Four_Parts_Non_Strict()
        {
            var engine = BuildPOLEngine();
            
            // Test with 4 parts (non-strict should truncate to 3)
            var row = new RowData(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["A"] = "CHANEL:MENS:TESTER:T123"
            });

            var bag = engine.Parse(row);
            
            Console.WriteLine("Test: 4 parts, non-strict mode");
            Console.WriteLine("Final PropertyBag values:");
            foreach (var kvp in bag.Values)
            {
                Console.WriteLine($"  {kvp.Key} = '{kvp.Value}'");
            }
            
            // With strict mode in config, invalid parts count leaves assignments empty
            Assert.Equal(string.Empty, bag.Values.GetValueOrDefault("Product.Brand", string.Empty));
            Assert.Equal(string.Empty, bag.Values.GetValueOrDefault("Offer.Ref", string.Empty));
        }
    }
}
