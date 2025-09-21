using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Xunit;

namespace ParsingEngine.Tests
{
    public class SimpleAssignActionTests
    {
        private static string LoadData(string name)
        {
            var baseDir = AppContext.BaseDirectory;
            var path = Path.Combine(baseDir, "TestData", "SimpleAssignAction", name + ".txt");
            if (!File.Exists(path)) throw new FileNotFoundException(path);
            return File.ReadAllText(path);
        }

        [Theory]
        [InlineData("hello", "hello")]
        [InlineData("empty", "")]
        public void Assign_Copies_Value(string dataFile, string expected)
        {
            var input = LoadData(dataFile);
            var bag = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Src"] = input };
            var act = new SimpleAssignAction("Src", "Dst");
            var ok = act.Execute(bag, new CellContext("A", input, CultureInfo.InvariantCulture, new Dictionary<string, object?>()));
            Assert.True(ok);
            Assert.True(bag.ContainsKey("Dst[0]"));
            Assert.Equal(expected, bag["Dst[0]"]);
            Assert.Equal("1", bag["Dst.Length"]);
            Assert.Equal("true", bag["Dst.Valid"]);
        }
    }
}
