using System;
using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace ParsingEngine.Tests
{
    public class ChainExecuterTests
    {
        [Theory]
        [InlineData("D&G The One Wom EDP (75ml+BC 50ml)", "D&G The One Wom EDP")]
        public void Execute_Applies_Assignments_From_RuleConfig(string cellData, string expexted)
        {
            var rc = new RuleConfig
            {
                Assign = new Dictionary<string, string>
                {
                    ["Product.Name"] = "TestName",
                    ["Offer.Price"] = "9.99"
                },
                Actions = new List<ActionConfig>
                {
                    new ActionConfig
                    {
                        Op = "Find",
                        Input = "Text",
                        Output = "AfterSize",
                        Parameters = new Dictionary<string,string> {{"options","remove,all" },{"pattern", @"(([^)]+))" } }
                    }
                }
            };
            var cfg = new ParserConfig();
            var executer = new ChainExecuter(rc, cfg);

            var ctx = new CellContext("A", cellData, CultureInfo.InvariantCulture, new Dictionary<string, object?>());
            var result = executer.Execute(ctx);

            Assert.True(result.Matched);
            Assert.Contains(result.Assignments, a => a.Property == "Product.Name" && (a.Value?.ToString() ?? string.Empty) == "TestName");
            Assert.Contains(result.Assignments, a => a.Property == "Offer.Price" && (a.Value?.ToString() ?? string.Empty) == "9.99");
            Assert.Contains(result.Assignments, a => a.Property == "AfterSize" && (a.Value?.ToString() ?? string.Empty) == expexted);

        }

        [Fact]
        public void Execute_No_Assignments_Returns_NotMatched()
        {
            var rc = new RuleConfig();
            var cfg = new ParserConfig();
            var executer = new ChainExecuter(rc, cfg);

            var ctx = new CellContext("A", "", CultureInfo.InvariantCulture, new Dictionary<string, object?>());
            var result = executer.Execute(ctx);

            Assert.False(result.Matched);
            Assert.Empty(result.Assignments);
        }
    }
}
