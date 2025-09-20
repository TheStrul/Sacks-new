using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Xunit;

namespace ParsingEngine.Tests
{
    public class ActionsTests
    {
        [Theory]
        [InlineData("hello", "hello")]
        [InlineData("", "")]
        [InlineData("123", "123")]
        public void SimpleAssignAction_Copies_Value(string input, string expected)
        {
            var bag = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Src"] = input
            };
            var act = new SimpleAssignAction("Src", "Dst");
            var ok = act.Execute(bag, new CellContext("A", input, CultureInfo.InvariantCulture, new Dictionary<string, object?>()));
            Assert.True(ok);
            Assert.True(bag.ContainsKey("Dst[3]"));
            Assert.Equal(expected, bag["Dst[3]"]); // result stored starting at index 3
            Assert.Equal("1", bag["Dst[1]"]); // length
            Assert.Equal("true", bag["Dst[2]"]); // valid
        }

        public static IEnumerable<object[]> RemoveActionData()
        {
            yield return new object[] { "D&G The One Wom EDP (75ml+BC 50ml)", "D&G The One Wom EDP", @"(([^)]+))", new string[] { "first", "remove" } };
        }

        [Theory]
        [MemberData(nameof(RemoveActionData))]
        public void RemoveAction_Removes_Pattern(string input, string expected, string pattern, List<string> options)
        {
            var bag = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Src"] = input
            };
            // Use FindAction with remove+all to remove all digits
            var act = new FindAction("Src", "Clean", pattern, options);
            var ok = act.Execute(bag, new CellContext("A", input, CultureInfo.InvariantCulture, new Dictionary<string, object?>()));
            Assert.True(ok);
            // cleaned value available at Clean.Clean
            Assert.True(bag.ContainsKey("Clean.Clean"));
            Assert.Equal(expected, bag["Clean.Clean"]);
        }

        [Theory]
        [InlineData("abc123def", "123")]
        [InlineData("no digits here", "")]
        [InlineData("x9y8", "9")]
        public void FindAction_Finds_First_Match_And_Groups(string input, string expectedWhole)
        {
            var bag = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Src"] = input
            };
            var act = new FindAction("Src", "Found", "(?<num>\\d+)", new List<string>() {"first" });
            var ok = act.Execute(bag, new CellContext("A", input, CultureInfo.InvariantCulture, new Dictionary<string, object?>()));
            if (string.IsNullOrEmpty(expectedWhole))
            {
                Assert.False(ok);
                Assert.Equal("0", bag["Found[1]"]); // no results
                Assert.Equal("false", bag["Found[2]"]);
            }
            else
            {
                Assert.True(ok);
                Assert.Equal(expectedWhole, bag["Found[3]"]); // single result placed at index 3
                Assert.Equal(expectedWhole, bag["Found.3.num"]); // named group stored at Found.3.num
            }
        }

        [Fact]
        public void FindAndRemoveAction_Cleans_And_Lists_Removed()
        {
            var bag = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Src"] = "abc123def45"
            };
            // Use FindAction to remove all digit sequences
            var act = new FindAction("Src", "Parts", "\\d+", new List<string>() { "all", "remove" });
            var ok = act.Execute(bag, new CellContext("A", bag["Src"], CultureInfo.InvariantCulture, new Dictionary<string, object?>()));
            Assert.True(ok);
            // cleaned value is at Parts.Clean
            Assert.Equal("abcdef", bag["Parts.Clean"]);
            // removed matches are at Parts[3], Parts[4]
            Assert.Equal("123", bag["Parts[3]"]);
            Assert.Equal("45", bag["Parts[4]"]);
            // length should be 2
            Assert.Equal("2", bag["Parts[1]"]);
            Assert.Equal("true", bag["Parts[2]"]); // valid
        }

        [Theory]
        [InlineData("Acme UPTOWN BRAND Co", "Acme Co", "UPTOWN|BRAND")]
        [InlineData("No Capitals here", "No Capitals here", "")]
        public void FindAndRemoveAction_Removes_All_Capital_Words(string input, string expectedCleanNormalized, string expectedRemovedPipe)
        {
            var bag = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Src"] = input
            };
            // Pattern: Unicode uppercase words, remove all
            var act = new FindAction("Src", "Parts", "\\b\\p{Lu}+\\b", new List<string>(){ "all", "remove" });
            var ok = act.Execute(bag, new CellContext("A", bag["Src"], CultureInfo.InvariantCulture, new Dictionary<string, object?>()));
            Assert.True(bag.ContainsKey("Parts.Clean"));
            var actualClean = bag["Parts.Clean"] ?? string.Empty;
            // Normalize whitespace for comparison
            var normActual = Regex.Replace(actualClean, "\\s+", " ").Trim();
            Assert.Equal(expectedCleanNormalized, normActual);

            var expectedRemoved = string.IsNullOrEmpty(expectedRemovedPipe) ? Array.Empty<string>() : expectedRemovedPipe.Split('|');
            if (expectedRemoved.Length == 0)
            {
                Assert.Equal("0", bag["Parts[1]"]); // no removed matches
                Assert.Equal("false", bag["Parts[2]"]); // valid false
            }
            else
            {
                for (int i = 0; i < expectedRemoved.Length; i++)
                {
                    var key = $"Parts[{i + 3}]";
                    Assert.True(bag.ContainsKey(key));
                    Assert.Equal(expectedRemoved[i], bag[key]);
                }
                Assert.Equal((expectedRemoved.Length).ToString(CultureInfo.InvariantCulture), bag["Parts[1]"]);
                Assert.Equal("true", bag["Parts[2]"]); // valid true
            }
        }
    }
}
