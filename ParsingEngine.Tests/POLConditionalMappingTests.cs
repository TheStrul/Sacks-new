using ParsingEngine;
using Xunit;
using System.Collections.Generic;
using System;

namespace ParsingEngine.Tests
{
    public class POLConditionalMappingTests
    {
        private ParserEngine BuildPOLEngine()
        {
            // Create POL config with ConditionalMapping
            var config = new ParserConfig
            {
                Lookups = new Dictionary<string, Dictionary<string, string>>
                {
                    ["genderMappings"] = new Dictionary<string, string>
                    {
                        ["MENS"] = "M",
                        ["WOMENS"] = "W", 
                        ["UNISEX"] = "U",
                        [""] = ""
                    },
                    ["typeMappings"] = new Dictionary<string, string>
                    {
                        ["TESTER"] = "Tester",
                        ["GIFTSET"] = "Gift Set",
                        ["MINI"] = "Mini",
                        [""] = ""
                    }
                },
                Columns = new List<ColumnConfig>
                {
                    new ColumnConfig
                    {
                        Column = "A",
                        Rule = new RuleConfig
                        {
                            Steps = new List<PipelineStep>
                            {
                                new PipelineStep { Op = "Trim" },
                                new PipelineStep { 
                                    Op = "SplitByDelimiter", 
                                    From = "Text", 
                                    Delimiter = ":", 
                                    OutputProperty = "Parts",
                                    ExpectedParts = 3,
                                    Strict = false
                                },
                                new PipelineStep { Op = "Assign", From = "Parts[0]", To = "Product.Brand" },
                                new PipelineStep { 
                                    Op = "ConditionalMapping", 
                                    From = "Parts[1]", 
                                    Delimiter = "|", 
                                    Mappings = new List<SplitMapping>
                                    {
                                        new SplitMapping { Table = "genderMappings", AssignTo = "Product.Gender" },
                                        new SplitMapping { Table = "typeMappings", AssignTo = "Product.Type" }
                                    }
                                },
                                new PipelineStep { Op = "Assign", From = "Parts[2]", To = "Offer.Ref" }
                            }
                        }
                    }
                }
            };

            return new ParserEngine(config);
        }

        [Fact]
        public void POL_ConditionalMapping_Combined_Gender_Type()
        {
            var engine = BuildPOLEngine();
            
            // Test: BULGARI:WOMENS|GIFTSET:41899
            var row = new RowData(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["A"] = "BULGARI:WOMENS|GIFTSET:41899"
            });

            var bag = engine.Parse(row);
            
            Console.WriteLine("POL Test: Combined Gender and Type");
            Console.WriteLine("Input: BULGARI:WOMENS|GIFTSET:41899");
            Console.WriteLine("Parsed values:");
            foreach (var kvp in bag.Values)
            {
                Console.WriteLine($"  {kvp.Key} = '{kvp.Value}'");
            }
            
            Assert.Equal("BULGARI", bag.Values["Product.Brand"]);
            Assert.Equal("W", bag.Values["Product.Gender"]);
            Assert.Equal("Gift Set", bag.Values["Product.Type"]);
            Assert.Equal("41899", bag.Values["Offer.Ref"]);
        }

        [Fact]
        public void POL_ConditionalMapping_Only_Gender()
        {
            var engine = BuildPOLEngine();
            
            // Test: CHANEL:MENS:T123
            var row = new RowData(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["A"] = "CHANEL:MENS:T123"
            });

            var bag = engine.Parse(row);
            
            Console.WriteLine("POL Test: Only Gender");
            Console.WriteLine("Input: CHANEL:MENS:T123");
            Console.WriteLine("Parsed values:");
            foreach (var kvp in bag.Values)
            {
                Console.WriteLine($"  {kvp.Key} = '{kvp.Value}'");
            }
            
            Assert.Equal("CHANEL", bag.Values["Product.Brand"]);
            Assert.Equal("M", bag.Values["Product.Gender"]);
            Assert.False(bag.Values.ContainsKey("Product.Type")); // No type found
            Assert.Equal("T123", bag.Values["Offer.Ref"]);
        }

        [Fact]
        public void POL_ConditionalMapping_Only_Type()
        {
            var engine = BuildPOLEngine();
            
            // Test: DIOR:TESTER:D456
            var row = new RowData(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["A"] = "DIOR:TESTER:D456"
            });

            var bag = engine.Parse(row);
            
            Console.WriteLine("POL Test: Only Type");
            Console.WriteLine("Input: DIOR:TESTER:D456");
            Console.WriteLine("Parsed values:");
            foreach (var kvp in bag.Values)
            {
                Console.WriteLine($"  {kvp.Key} = '{kvp.Value}'");
            }
            
            Assert.Equal("DIOR", bag.Values["Product.Brand"]);
            Assert.False(bag.Values.ContainsKey("Product.Gender")); // No gender found
            Assert.Equal("Tester", bag.Values["Product.Type"]);
            Assert.Equal("D456", bag.Values["Offer.Ref"]);
        }

        [Fact]
        public void POL_ConditionalMapping_Empty_Middle()
        {
            var engine = BuildPOLEngine();
            
            // Test: YSL::Y789
            var row = new RowData(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["A"] = "YSL::Y789"
            });

            var bag = engine.Parse(row);
            
            Console.WriteLine("POL Test: Empty Middle Part");
            Console.WriteLine("Input: YSL::Y789");
            Console.WriteLine("Parsed values:");
            foreach (var kvp in bag.Values)
            {
                Console.WriteLine($"  {kvp.Key} = '{kvp.Value}'");
            }
            
            Assert.Equal("YSL", bag.Values["Product.Brand"]);
            Assert.False(bag.Values.ContainsKey("Product.Gender"));
            Assert.False(bag.Values.ContainsKey("Product.Type"));
            Assert.Equal("Y789", bag.Values["Offer.Ref"]);
        }
    }
}