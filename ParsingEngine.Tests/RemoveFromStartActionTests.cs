using System;
using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace ParsingEngine.Tests
{
    public class RemoveFromStartActionTests
    {
        [Fact]
        public void RemoveFromStartAction_Removes_First_Matching_Word()
        {
            var bag = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Src"] = "D&G The One Wom EDP (75ml+BC 50ml)"
            };

            // pattern: word that contains only uppercase letters or & and dots/apostrophes etc
            var pattern = "^[A-Z&.'\"]+$"; // simple example: uppercase + & . ' "
            var act = new RemoveFromStartAction("Src", "Parts", pattern);
            var ok = act.Execute(bag, new CellContext("A", bag["Src"], CultureInfo.InvariantCulture, new Dictionary<string, object?>()));

            Assert.True(ok);
            // removed word should be at Parts[0]
            Assert.Equal("D&G", bag["Parts[0]"]);
            // cleaned string should be at Parts.Clean
            Assert.True(bag.ContainsKey("Parts.Clean"));
            Assert.StartsWith("The One Wom", bag["Parts.Clean"]);
            Assert.Equal("1", bag["Parts.Length"]);
            Assert.Equal("true", bag["Parts.Status"]);
        }
    }
}
