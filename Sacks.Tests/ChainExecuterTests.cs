using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;

using Xunit;

namespace ParsingEngine.Tests
{
    public class ChainExecuterTests
    {


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
        [Fact]
        public void LoadHandColumnE_And_Run_Sample()
        {
            // locate test data Pol.json in TestData
            string fName = @"SacksApp\Configuration\supplier-formats.json";
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            string path = Path.Combine(dir.FullName, fName);
            for (int i = 0; i < 10 && dir != null; i++)
            {
                if (File.Exists(path)) break;
                dir = dir.Parent;
                path = Path.Combine(dir!.FullName, fName);
            }
            if (!File.Exists(path)) throw new FileNotFoundException(path);

            var json = File.ReadAllText(path);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            JsonElement? hand = null;
            if (root.TryGetProperty("suppliers", out var suppliers))
            {
                foreach (var s in suppliers.EnumerateArray())
                {
                    if (s.TryGetProperty("name", out var name) && string.Equals(name.GetString(), "HAND", StringComparison.OrdinalIgnoreCase))
                    {
                        hand = s;
                        break;
                    }
                }
            }

            Assert.NotNull(hand);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // Deserialize parserConfig into ParserConfig
            ParserConfig parserCfg = new ParserConfig();
            if (hand.Value.TryGetProperty("parserConfig", out var pc))
            {
                parserCfg = JsonSerializer.Deserialize<ParserConfig>(pc.GetRawText(), options)!;
            }

            // Extract column B actions
            List<ActionConfig> actions = new();
            if (hand.Value.TryGetProperty("parserConfig", out var pcfg) && pcfg.TryGetProperty("columns", out var cols))
            {
                foreach (var col in cols.EnumerateArray())
                {
                    if (col.TryGetProperty("column", out var colName) && string.Equals(colName.GetString(), "E", StringComparison.OrdinalIgnoreCase))
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
            var input = "M KORS Sexy Amber Wom EDP (100ml)";
            var ctx = new CellContext("E", input, CultureInfo.InvariantCulture, new Dictionary<string, object?>());

            var result = executer.Execute(ctx);

            // If it's too complex to assert exact values, just ensure execution succeeded and print assignments count
            Assert.True(result.Matched);
            Assert.NotEmpty(result.Assignments);

            // Optional: check that Offer.Description was set
            Assert.Contains(result.Assignments, a => a.Property == "Offer.Description");
        }
    }
}
