using ParsingEngine;
using Xunit;
using System.Collections.Generic;
using System;

namespace ParsingEngine.Tests
{
    public class SplitByDelimiterStrictTests
    {
        [Fact]
        public void SplitByDelimiter_Strict_Mode_Wrong_Parts_Count()
        {
            // Create a test config with strict mode
            var config = new ParserConfig
            {
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
                                    Input = "Text", 
                                    Delimiter = ":", 
                                    Output = "Parts",
                                    ExpectedParts = 3,
                                    Strict = true // Enable strict mode
                                },
                                new PipelineStep { Op = "Assign", Input = "Parts[0]", Output = "Brand" },
                                new PipelineStep { Op = "Assign", Input = "Parts[1]", Output = "Gender" },
                                new PipelineStep { Op = "Assign", Input = "Parts[2]", Output = "Ref" }
                            }
                        }
                    }
                }
            };

            var engine = new ParserEngine(config);
            
            // Test with only 2 parts (should fail in strict mode)
            var row = new RowData(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["A"] = "CHANEL:MENS"  // Only 2 parts, but expects 3
            });

            var bag = engine.Parse(row);
            
            Console.WriteLine("Test: Strict mode with wrong parts count");
            Console.WriteLine("Final PropertyBag values:");
            foreach (var kvp in bag.Values)
            {
                Console.WriteLine($"  {kvp.Key} = '{kvp.Value}'");
            }
            
            // In strict mode, should fail and not extract any brands/refs
            Assert.True(!bag.Values.ContainsKey("Brand") || string.IsNullOrEmpty(bag.Values["Brand"]?.ToString()));
        }

        [Fact]
        public void SplitByDelimiter_Strict_Mode_Correct_Parts_Count()
        {
            // Create a test config with strict mode
            var config = new ParserConfig
            {
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
                                    Input = "Text", 
                                    Delimiter = ":",
                                    Output = "Parts",
                                    ExpectedParts = 3,
                                    Strict = true // Enable strict mode
                                },
                                new PipelineStep { Op = "Assign", Input = "Parts[0]", Output = "Brand" },
                                new PipelineStep { Op = "Assign", Input = "Parts[1]", Output = "Gender" },
                                new PipelineStep { Op = "Assign", Input = "Parts[2]", Output = "Ref" }
                            }
                        }
                    }
                }
            };

            var engine = new ParserEngine(config);
            
            // Test with exactly 3 parts (should succeed in strict mode)
            var row = new RowData(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["A"] = "CHANEL:MENS:T123"  // Exactly 3 parts
            });

            var bag = engine.Parse(row);
            
            Console.WriteLine("Test: Strict mode with correct parts count");
            Console.WriteLine("Final PropertyBag values:");
            foreach (var kvp in bag.Values)
            {
                Console.WriteLine($"  {kvp.Key} = '{kvp.Value}'");
            }
            
            // In strict mode with correct parts, should work normally
            Assert.Equal("CHANEL", bag.Values["Brand"]);
            Assert.Equal("MENS", bag.Values["Gender"]);
            Assert.Equal("T123", bag.Values["Ref"]);
        }
    }
}
