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
            var pb = new PropertyBag();
            pb.SetVariable("Src", input);
            var act = new SimpleAssignAction("Src", "Dst");
            var ok = act.Execute(new CellContext("A", input, CultureInfo.InvariantCulture, pb));
            Assert.True(ok);
            Assert.True(pb.Variables.ContainsKey("Dst[0]"));
            Assert.Equal(expected, pb.Variables["Dst[0]"]);
            Assert.Equal("1", pb.Variables["Dst.Length"]);
            Assert.Equal("true", pb.Variables["Dst.Valid"]);
        }
    }
}
