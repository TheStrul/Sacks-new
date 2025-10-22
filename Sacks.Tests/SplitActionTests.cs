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
            var pb = new PropertyBag();
            pb.SetVariable("Src", input);
            var act = new SplitAction("Src", "Parts", ",");
            var ok = act.Execute(new CellContext("A", input, CultureInfo.InvariantCulture, pb));
            if (expectedCount == 0)
            {
                Assert.False(ok);
                Assert.Equal("0", pb.Variables["Parts.Length"]);
                Assert.Equal("false", pb.Variables["Parts.Valid"]);
            }
            else
            {
                Assert.True(ok);
                Assert.Equal(expectedCount.ToString(CultureInfo.InvariantCulture), pb.Variables["Parts.Length"]);
                Assert.Equal("true", pb.Variables["Parts.Valid"]);
            }
        }
    }
}
