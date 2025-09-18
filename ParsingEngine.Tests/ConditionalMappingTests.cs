using ParsingEngine;
using Xunit;
using System.Collections.Generic;
using System;

namespace ParsingEngine.Tests
{
    public class ConditionalMappingTests
    {
        [Fact]
        public void ConditionalMapping_Gender_And_Type_Separation()
        {
            // Create a test config with ConditionalMapping
            var config = new ParserConfig
            {
                Lookups = new Dictionary<string, Dictionary<string, string>>
                {
                    ["genderMappings"] = new Dictionary<string, string>
                    {
                        ["MENS"] = "M",
                        ["WOMENS"] = "W", 
                        ["UNISEX"] = "U",
                        [""] = "" // Fallback for unmatched
                    },
                    ["typeMappings"] = new Dictionary<string, string>
                    {
                        ["TESTER"] = "Tester",
                        ["GIFTSET"] = "Gift Set",
                        ["MINI"] = "Mini",
                        [""] = "" // Fallback for unmatched
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

            var engine = new ParserEngine(config);
            
            // Test with combined gender and type: "MENS|TESTER"
            var row = new RowData(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["A"] = "CHANEL:MENS|TESTER:T123"
            });

            var bag = engine.Parse(row);
            
            Console.WriteLine("Test: ConditionalMapping with Gender and Type");
            Console.WriteLine("Final PropertyBag values:");
            foreach (var kvp in bag.Values)
            {
                Console.WriteLine($"  {kvp.Key} = '{kvp.Value}'");
            }
            
            // Verify the parsing results
            Assert.Equal("CHANEL", bag.Values["Product.Brand"]);
            Assert.Equal("M", bag.Values["Product.Gender"]);
            Assert.Equal("Tester", bag.Values["Product.Type"]);
            Assert.Equal("T123", bag.Values["Offer.Ref"]);
        }

        [Fact]
        public void ConditionalMapping_Only_Gender()
        {
            // Test with only gender: "WOMENS"
            var config = new ParserConfig
            {
                Lookups = new Dictionary<string, Dictionary<string, string>>
                {
                    ["genderMappings"] = new Dictionary<string, string>
                    {
                        ["MENS"] = "M",
                        ["WOMENS"] = "W", 
                        ["UNISEX"] = "U"
                    },
                    ["typeMappings"] = new Dictionary<string, string>
                    {
                        ["TESTER"] = "Tester",
                        ["GIFTSET"] = "Gift Set"
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

            var engine = new ParserEngine(config);
            
            var row = new RowData(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["A"] = "DIOR:WOMENS:D456"
            });

            var bag = engine.Parse(row);
            
            // Should have gender but no type
            Assert.Equal("DIOR", bag.Values["Product.Brand"]);
            Assert.Equal("W", bag.Values["Product.Gender"]);
            Assert.False(bag.Values.ContainsKey("Product.Type")); // Should not be assigned
            Assert.Equal("D456", bag.Values["Offer.Ref"]);
        }

        [Fact]
        public void ConditionalMapping_Empty_Parts()
        {
            // Test with empty Parts[1]
            var config = new ParserConfig
            {
                Lookups = new Dictionary<string, Dictionary<string, string>>
                {
                    ["genderMappings"] = new Dictionary<string, string>
                    {
                        ["MENS"] = "M",
                        ["WOMENS"] = "W"
                    },
                    ["typeMappings"] = new Dictionary<string, string>
                    {
                        ["TESTER"] = "Tester"
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

            var engine = new ParserEngine(config);
            
            var row = new RowData(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["A"] = "YSL::Y789"  // Empty middle part
            });

            var bag = engine.Parse(row);
            
            // Should parse brand and ref, but no gender or type
            Assert.Equal("YSL", bag.Values["Product.Brand"]);
            Assert.False(bag.Values.ContainsKey("Product.Gender"));
            Assert.False(bag.Values.ContainsKey("Product.Type"));
            Assert.Equal("Y789", bag.Values["Offer.Ref"]);
        }
    }
}