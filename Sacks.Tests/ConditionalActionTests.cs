using System;
using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace ParsingEngine.Tests
{
    public class ConditionalActionTests
    {
        [Fact]
        public void ConditionalAction_Performs_Assign_When_Condition_Matches()
        {
            var bag = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Text"] = "hello",
                ["Parts.Length"] = "2",
                ["Parts[0]"] = "A",
                ["Parts[1]"] = "B"
            };

            var act = new ConditionalAssignAction("Text", "Out", "Parts.Length == 2", true);
            var ok = act.Execute(bag, new CellContext("A", bag["Text"], CultureInfo.InvariantCulture, new Dictionary<string, object?>()));
            Assert.True(ok);
            Assert.Equal("hello", bag["Out[3]"]); // result written at index 3 by ActionHelpers
            Assert.Equal("hello", bag["Out.Clean"]);
        }

        [Fact]
        public void ConditionalAction_Skips_When_Condition_Not_Match()
        {
            var bag = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Text"] = "hello",
                ["Parts.Length"] = "1",
                ["Parts[0]"] = "A"
            };

            var act = new ConditionalAssignAction("Text", "Out", "Parts.Length == 2", true);
            var ok = act.Execute(bag, new CellContext("A", bag["Text"], CultureInfo.InvariantCulture, new Dictionary<string, object?>()));
            Assert.False(ok);
            Assert.False(bag.ContainsKey("Out[3]"));
        }
    }
}
