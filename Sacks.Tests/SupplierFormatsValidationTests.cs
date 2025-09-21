using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace Sacks.Tests.Configuration
{
    public class SupplierFormatsValidationTests
    {
        private static int ColumnLettersToIndex(string letters)
        {
            if (string.IsNullOrEmpty(letters)) return 0;
            int col = 0;
            foreach (var ch in letters)
            {
                if (ch < 'A' || ch > 'Z') continue;
                col = col * 26 + (ch - 'A' + 1);
            }
            return col;
        }

        [Fact]
        public void SupplierColumns_In_Config_Exist_In_SampleFile()
        {
            // locate supplier-formats.json
            var repoRoot = AppContext.BaseDirectory;
            // project output paths put tests in bin/.../ so go up until we find the repo root that contains SacksApp/Configuration
            string? current = repoRoot;
            string? configPath = null;
            while (current != null)
            {
                var candidate = Path.Combine(current, "SacksApp", "Configuration", "supplier-formats.json");
                if (File.Exists(candidate))
                {
                    configPath = candidate;
                    break;
                }
                var parent = Directory.GetParent(current);
                current = parent?.FullName;
            }

            if (configPath == null)
            {
                // skip test when config not available in environment
                return;
            }

            var json = File.ReadAllText(configPath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var suppliers = root.GetProperty("suppliers");

            JsonElement? pol = null;
            foreach (var s in suppliers.EnumerateArray())
            {
                if (s.TryGetProperty("name", out var name) && name.GetString()?.Equals("POL", StringComparison.OrdinalIgnoreCase) == true)
                {
                    pol = s;
                    break;
                }
            }

            if (pol == null)
            {
                // no POL supplier defined - fail
                throw new Xunit.Sdk.XunitException("POL supplier not found in supplier-formats.json");
            }

            // extract columns defined in the config
            var columns = new List<string>();
            if (pol.Value.TryGetProperty("parserConfig", out var parser) && parser.TryGetProperty("columns", out var cols))
            {
                foreach (var c in cols.EnumerateArray())
                {
                    if (c.TryGetProperty("column", out var colProp))
                    {
                        var col = colProp.GetString();
                        if (!string.IsNullOrWhiteSpace(col)) columns.Add(col.Trim());
                    }
                }
            }

            // get expectedColumnCount from fileStructure if present
            int expectedColumnCount = -1;
            if (pol.Value.TryGetProperty("fileStructure", out var fs) && fs.TryGetProperty("expectedColumnCount", out var ecc))
            {
                expectedColumnCount = ecc.GetInt32();
            }

            // locate sample xlsx (Inputs/POL 31.8.25.xlsx) by walking up from repoRoot
            string? samplePath = null;
            current = repoRoot;
            while (current != null)
            {
                var candidate = Path.Combine(current, "Inputs", "POL 31.8.25.xlsx");
                if (File.Exists(candidate))
                {
                    samplePath = candidate;
                    break;
                }
                var parent = Directory.GetParent(current);
                current = parent?.FullName;
            }

            if (samplePath == null)
            {
                // sample file not available — assert config columns are syntactically valid but skip file checks
                Assert.NotEmpty(columns);
                return;
            }

            // open xlsx as zip and inspect xl/worksheets/sheet1.xml to find used columns
            using var z = ZipFile.OpenRead(samplePath);
            var sheetEntry = z.GetEntry("xl/worksheets/sheet1.xml");
            Assert.NotNull(sheetEntry);

            string sheetXml;
            using (var s = sheetEntry.Open())
            using (var sr = new StreamReader(s))
            {
                sheetXml = sr.ReadToEnd();
            }

            // try to read dimension tag first: <dimension ref="A1:E123"/>
            var dimMatch = Regex.Match(sheetXml, "<dimension[^>]*ref=\"(?<r>[A-Z0-9:]+)\"", RegexOptions.IgnoreCase);
            int maxColIndex = 0;
            if (dimMatch.Success)
            {
                var refVal = dimMatch.Groups["r"].Value;
                // ref can be A1 or A1:E10 or similar
                var parts = refVal.Split(':');
                var last = parts.Length > 1 ? parts[^1] : parts[0];
                var colLetters = new string(last.TakeWhile(char.IsLetter).ToArray());
                maxColIndex = ColumnLettersToIndex(colLetters);
            }
            else
            {
                // fallback: scan all cell refs r="A1"
                var matches = Regex.Matches(sheetXml, @"r=""(?<c>[A-Z]+)\d+""", RegexOptions.IgnoreCase);
                foreach (Match m in matches)
                {
                    var col = m.Groups["c"].Value.ToUpperInvariant();
                    var idx = ColumnLettersToIndex(col);
                    if (idx > maxColIndex) maxColIndex = idx;
                }
            }

            Assert.True(maxColIndex > 0, "Could not determine used columns in sample file");

            // validate each column defined in config exists within used columns
            foreach (var c in columns)
            {
                var colLetters = c.Trim().ToUpperInvariant();
                var idx = ColumnLettersToIndex(colLetters);
                Assert.True(idx > 0, $"Invalid column letter '{c}' in config");
                Assert.True(idx <= maxColIndex, $"Config column '{c}' (index {idx}) is not present in sample file (max column {maxColIndex})");
            }

            if (expectedColumnCount > 0)
            {
                Assert.True(expectedColumnCount <= maxColIndex, $"fileStructure.expectedColumnCount ({expectedColumnCount}) is greater than actual max column in sample file ({maxColIndex})");
                // also ensure config doesn't declare more columns than expectedColumnCount
                Assert.True(columns.Count <= expectedColumnCount, $"Number of columns defined in config ({columns.Count}) exceeds fileStructure.expectedColumnCount ({expectedColumnCount})");
            }
        }
    }
}
