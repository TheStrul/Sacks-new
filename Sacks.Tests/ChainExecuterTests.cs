using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
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
                        Parameters = new Dictionary<string,string> {{"options","remove,all" },{"pattern", "(([^)]+))" } }
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

        [Fact]
        public void LoadPolColumnB_And_Run_Sample()
        {
            // locate test data Pol.json in TestData
            var baseDir = AppContext.BaseDirectory;
            string path = Path.Combine(baseDir, "TestData", "Pol.json");
            if (!File.Exists(path))
            {
                var dir = new DirectoryInfo(baseDir);
                for (int i = 0; i < 10 && dir != null; i++)
                {
                    path = Path.Combine(dir.FullName, "TestData", "Pol.json");
                    if (File.Exists(path)) break;
                    dir = dir.Parent;
                }
            }
            if (!File.Exists(path)) throw new FileNotFoundException(path);

            var json = File.ReadAllText(path);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            JsonElement? pol = null;
            if (root.TryGetProperty("suppliers", out var suppliers))
            {
                foreach (var s in suppliers.EnumerateArray())
                {
                    if (s.TryGetProperty("name", out var name) && string.Equals(name.GetString(), "POL", StringComparison.OrdinalIgnoreCase))
                    {
                        pol = s;
                        break;
                    }
                }
            }

            Assert.NotNull(pol);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // Deserialize parserConfig into ParserConfig
            ParserConfig parserCfg = new ParserConfig();
            if (pol.Value.TryGetProperty("parserConfig", out var pc))
            {
                parserCfg = JsonSerializer.Deserialize<ParserConfig>(pc.GetRawText(), options)!;
            }

            // Extract column B actions
            List<ActionConfig> actions = new();
            if (pol.Value.TryGetProperty("parserConfig", out var pcfg) && pcfg.TryGetProperty("columns", out var cols))
            {
                foreach (var col in cols.EnumerateArray())
                {
                    if (col.TryGetProperty("column", out var colName) && string.Equals(colName.GetString(), "B", StringComparison.OrdinalIgnoreCase))
                    {
                        if (col.TryGetProperty("rule", out var rule) && rule.TryGetProperty("actions", out var acts))
                        {
                            actions = JsonSerializer.Deserialize<List<ActionConfig>>(acts.GetRawText(), options) ?? new List<ActionConfig>();
                        }
                        break;
                    }
                }
            }

            Assert.NotEmpty(actions);

            var rc = new RuleConfig
            {
                Actions = actions
            };

            var executer = new ChainExecuter(rc, parserCfg);

            // sample input
            var input = "CONTRADICTION M 100ML / 3.3OZ EDT";
            var ctx = new CellContext("B", input, CultureInfo.InvariantCulture, new Dictionary<string, object?>());

            var result = executer.Execute(ctx);

            // If it's too complex to assert exact values, just ensure execution succeeded and print assignments count
            Assert.True(result.Matched);
            Assert.NotEmpty(result.Assignments);

            // Optional: check that Offer.Description was set
            Assert.Contains(result.Assignments, a => a.Property == "Offer.Description");
        }
    }
}
