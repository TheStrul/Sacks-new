using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Xunit;

namespace ParsingEngine.Tests
{
    public class SplitActionTests
    {
        private static string LoadData(string name)
        {
            var baseDir = AppContext.BaseDirectory;
            var path = Path.Combine(baseDir, "TestData", "SplitAction", name + ".txt");
            if (!File.Exists(path)) throw new FileNotFoundException(path);
            return File.ReadAllText(path);
        }

        [Theory]
        [InlineData("comma-two", 2)]
        [InlineData("pipe-empty", 0)]
        public void Split_Splits_Correctly(string dataFile, int expectedCount)
        {
            var input = LoadData(dataFile);
            var bag = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Src"] = input };
            var act = new SplitAction("Src", "Parts", ",");
            var ok = act.Execute(bag, new CellContext("A", input, CultureInfo.InvariantCulture, new Dictionary<string, object?>()));
            if (expectedCount == 0)
            {
                Assert.False(ok);
                Assert.Equal("0", bag["Parts.Length"]);
                Assert.Equal("false", bag["Parts.Valid"]);
            }
            else
            {
                Assert.True(ok);
                Assert.Equal(expectedCount.ToString(CultureInfo.InvariantCulture), bag["Parts.Length"]);
                Assert.Equal("true", bag["Parts.Valid"]);
            }
        }
    }
}
