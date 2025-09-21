using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace ParsingEngine.Tests
{
    public class FindActionTests
    {
        // Test dataset files live under Sacks.Tests/TestData/FindAction
        private static string LoadData(string name)
        {
            var baseDir = AppContext.BaseDirectory;
            var path = Path.Combine(baseDir, "TestData", "FindAction", name + ".txt");
            if (File.Exists(path)) return File.ReadAllText(path);

            var dir = new DirectoryInfo(baseDir);
            for (int i = 0; i < 10 && dir != null; i++)
            {
                path = Path.Combine(dir.FullName, "TestData", "FindAction", name + ".txt");
                if (File.Exists(path)) return File.ReadAllText(path);
                dir = dir.Parent;
            }

            path = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "FindAction", name + ".txt");
            if (File.Exists(path)) return File.ReadAllText(path);

            throw new FileNotFoundException($"Test data file not found: {name}.txt searched under {AppContext.BaseDirectory}");
        }

        private static List<string> LoadDataLines(string name)
        {
            var baseDir = AppContext.BaseDirectory;
            var path = Path.Combine(baseDir, "TestData", "FindAction", name + ".txt");
            if (!File.Exists(path))
            {
                var dir = new DirectoryInfo(baseDir);
                for (int i = 0; i < 10 && dir != null; i++)
                {
                    path = Path.Combine(dir.FullName, "TestData", "FindAction", name + ".txt");
                    if (File.Exists(path)) break;
                    dir = dir.Parent;
                }
            }

            if (!File.Exists(path))
            {
                path = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "FindAction", name + ".txt");
            }

            if (!File.Exists(path)) throw new FileNotFoundException(path);

            return File.ReadAllLines(path).Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l)).ToList();
        }

        private static Dictionary<string, (string Clean, string RemovedPipe)> LoadExpectedMap(string name)
        {
            var baseDir = AppContext.BaseDirectory;
            var path = Path.Combine(baseDir, "TestData", "FindAction", name + ".expected.txt");
            if (!File.Exists(path))
            {
                var dir = new DirectoryInfo(baseDir);
                for (int i = 0; i < 10 && dir != null; i++)
                {
                    path = Path.Combine(dir.FullName, "TestData", "FindAction", name + ".expected.txt");
                    if (File.Exists(path)) break;
                    dir = dir.Parent;
                }
            }

            if (!File.Exists(path))
            {
                path = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "FindAction", name + ".expected.txt");
            }

            var map = new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase);
            if (!File.Exists(path)) return map;

            foreach (var line in File.ReadAllLines(path))
            {
                var t = line.Split('\t'); // input \t clean \t removedPipe
                if (t.Length >= 2)
                {
                    var input = t[0].Trim();
                    var clean = t.Length >= 2 ? t[1].Trim() : string.Empty;
                    var removed = t.Length >= 3 ? t[2].Trim() : string.Empty;
                    if (!string.IsNullOrEmpty(input)) map[input] = (clean, removed);
                }
            }
            return map;
        }

        [Theory]
        [InlineData("first-digits-1", "123")]
        [InlineData("first-digits-2", "")]
        public void Find_First_Match_And_Groups(string dataFile, string expectedWhole)
        {
            var lines = LoadDataLines(dataFile);
            var expectedParts = expectedWhole?.Split('|') ?? Array.Empty<string>();

            for (int idx = 0; idx < lines.Count; idx++)
            {
                var input = lines[idx];
                var bag = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase){ ["Src"] = input };
                var act = new FindAction("Src", "Found", "(?<num>\\d+)", new List<string>() { "first" });
                var ok = act.Execute(bag, new CellContext("A", input, CultureInfo.InvariantCulture, new Dictionary<string, object?>()));

                var expected = expectedParts.Length == lines.Count ? expectedParts[idx] : expectedParts.FirstOrDefault() ?? expectedWhole;

                if (string.IsNullOrEmpty(expected))
                {
                    Assert.False(ok);
                    Assert.Equal("0", bag["Found.Length"]);
                    Assert.Equal("false", bag["Found.Valid"]);
                }
                else
                {
                    Assert.True(ok);
                    Assert.Equal(expected, bag["Found[0]"]);
                    Assert.Equal(expected, bag["Found.3.num"]);
                }
            }
        }

        [Fact]
        public void Find_All_Remove_Cleans_And_Lists_Removed()
        {
            var lines = LoadDataLines("remove-digits");
            foreach (var input in lines)
            {
                var bag = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase){ ["Src"] = input };
                var act = new FindAction("Src", "Parts", "\\d+", new List<string>() { "all", "remove" });
                var ok = act.Execute(bag, new CellContext("A", input, CultureInfo.InvariantCulture, new Dictionary<string, object?>()));
                Assert.True(ok);
                Assert.Equal("abcdef", bag["Parts.Clean"]);
                Assert.Equal("123", bag["Parts[0]"]);
                Assert.Equal("45", bag["Parts[1]"]);
                Assert.Equal("2", bag["Parts.Length"]);
                Assert.Equal("true", bag["Parts.Valid"]);
            }
        }

        [Fact]
        public void Remove_Size_Oz_Pattern()
        {
            var lines = LoadDataLines("find-size-oz");
            foreach (var input in lines)
            {
                var bag = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase){ ["Src"] = input };
                // Match optional surrounding separators (slashes/spaces) and capture the size in a named group 'size'
                var pattern = @"(?i)\s*[\\/]?\s*(?<size>\d+(?:\.\d+)?\s*oz)";
                var act = new FindAction("Src", "Found", pattern, new List<string>() { "all" , "remove", "ignorecase" });
                var ok = act.Execute(bag, new CellContext("A", input, CultureInfo.InvariantCulture, new Dictionary<string, object?>()));
                Assert.True(ok);
                // Normalize results (whitespace, case) before asserting
                var raw = bag.TryGetValue("Found[0]", out var r) ? r ?? string.Empty : string.Empty;
                var norm = Regex.Replace(raw, "\\s+", " ").Trim().ToLowerInvariant();
                Assert.Equal("3.4 oz", norm);

                var rawGroup = bag.TryGetValue("Found.3.size", out var g) ? g ?? string.Empty : string.Empty;
                var normGroup = Regex.Replace(rawGroup, "\\s+", " ").Trim().ToLowerInvariant();
                Assert.Equal("3.4 oz", normGroup);
            }
        }

        [Fact]
        public void Find_Size_Ml_Pattern()
        {
            var lines = LoadDataLines("find-size-ml");
            foreach (var input in lines)
            {
                var bag = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase){ ["Src"] = input };
                var act = new FindAction("Src", "Found", "(?<size>\\d+\\s*ml)", new List<string>() { "first" });
                var ok = act.Execute(bag, new CellContext("A", input, CultureInfo.InvariantCulture, new Dictionary<string, object?>()));
                Assert.True(ok);
                Assert.Equal("250ml", bag["Found[0]"]);
                Assert.Equal("250ml", bag["Found.3.size"]);
            }
        }

        [Fact]
        public void Find_Words_With_Capital_Letters_Only()
        {
            var lines = LoadDataLines("find-capital-words");
            foreach (var input in lines)
            {
                var bag = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase){ ["Src"] = input };
                var act = new FindAction("Src", "Parts", @"^[A-Z]+(?:[.&][A-Z]+)*(?:\s+[A-Z]+(?:[.&][A-Z]+)*)*", new List<string>() { "first", "remove" });
                var ok = act.Execute(bag, new CellContext("A", input, CultureInfo.InvariantCulture, new Dictionary<string, object?>()));

                // No hard assertions here; if you want to assert on specific lines create an expected file
            }
        }

        [Fact]
        public void Remove_Words_With_Capital_Letters_Only()
        {
            var lines = LoadDataLines("find-capital-words");
            var expected = LoadExpectedMap("find-capital-words");

            foreach (var input in lines)
            {
                var bag = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase){ ["Src"] = input };
                var act = new FindAction("Src", "Parts", "\\b\\p{Lu}+\\b", new List<string>() { "all", "remove" });
                var ok = act.Execute(bag, new CellContext("A", input, CultureInfo.InvariantCulture, new Dictionary<string, object?>()));
                Assert.True(ok);
                Assert.True(bag.ContainsKey("Parts.Clean"));
                var actualClean = bag["Parts.Clean"] ?? string.Empty;
                var normActual = Regex.Replace(actualClean, "\\s+", " ").Trim();

                if (expected.TryGetValue(input, out var exp))
                {
                    if (!string.IsNullOrEmpty(exp.Clean))
                        Assert.Equal(exp.Clean, normActual);

                    if (!string.IsNullOrEmpty(exp.RemovedPipe))
                    {
                        var removed = exp.RemovedPipe.Split('|', StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i < removed.Length; i++)
                        {
                            var key = $"Parts[{i}]";
                            Assert.True(bag.ContainsKey(key));
                            Assert.Equal(removed[i], bag[key]);
                        }
                        Assert.Equal(removed.Length.ToString(CultureInfo.InvariantCulture), bag["Parts.Length"]);
                        Assert.Equal("true", bag["Parts.Valid"]);
                    }
                }
            }
        }

        [Fact]
        public void Find_Strings_Surrounded_With_Parentheses()
        {
            var lines = LoadDataLines("surrounded-parentheses");
            foreach (var input in lines)
            {
                var bag = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase){ ["Src"] = input };
                var act = new FindAction("Src", "Found", "\\(([^)]+)\\)", new List<string>() { "first" });
                var ok = act.Execute(bag, new CellContext("A", input, CultureInfo.InvariantCulture, new Dictionary<string, object?>()));
                Assert.True(ok);
                Assert.Equal("(75ml+BC 50ml)", bag["Found[0]"]);
            }
        }
    }
}
