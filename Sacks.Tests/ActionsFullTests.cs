using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Xunit;

namespace ParsingEngine.Tests
{
    public class ActionsFullTests
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
            Assert.True(bag.ContainsKey("Dst[0]"));
            Assert.Equal(expected, bag["Dst[0]"]); // result stored starting at index 0
            Assert.Equal("1", bag["Dst.Length"]); // length
            Assert.Equal("true", bag["Dst.Valid"]); // valid
        }

        [Theory]
        [InlineData(":", new[] { "a", "b", "c" }, 3)]
        [InlineData(",", new[] { "one", "two" }, 2)]
        [InlineData("|", new string[] { }, 0)]
        public void SplitAction_Splits_Correctly(string delimiter, string[] parts, int expectedCount)
        {
            parts ??= Array.Empty<string>();
            var input = string.Join(delimiter, parts);
            var bag = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Src"] = input };
            var act = new SplitAction("Src", "Parts", delimiter);
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
                for (int i = 0; i < expectedCount; i++)
                {
                    Assert.Equal(parts[i], bag[$"Parts[{i}]"]);
                }
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
            var act = new FindAction("Src", "Parts", "\\d+", new List<string>() { "all", "remove" },false, null);
            var ok = act.Execute(bag, new CellContext("A", bag["Src"], CultureInfo.InvariantCulture, new Dictionary<string, object?>()));
            Assert.True(ok);
            // cleaned value is at Parts.Clean
            Assert.Equal("abcdef", bag["Parts.Clean"]);
            // removed matches are at Parts[0], Parts[1]
            Assert.Equal("123", bag["Parts[0]"]);
            Assert.Equal("45", bag["Parts[1]"]);
            // length should be 2
            Assert.Equal("2", bag["Parts.Length"]);
            Assert.Equal("true", bag["Parts.Valid"]); // valid
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
            var act = new FindAction("Src", "Parts", "\\b\\p{Lu}+\\b", new List<string>(){ "all", "remove" },false, null);
            var ok = act.Execute(bag, new CellContext("A", bag["Src"], CultureInfo.InvariantCulture, new Dictionary<string, object?>()));
            Assert.True(bag.ContainsKey("Parts.Clean"));
            var actualClean = bag["Parts.Clean"] ?? string.Empty;
            // Normalize whitespace for comparison
            var normActual = Regex.Replace(actualClean, "\\s+", " ").Trim();
            Assert.Equal(expectedCleanNormalized, normActual);

            var expectedRemoved = string.IsNullOrEmpty(expectedRemovedPipe) ? Array.Empty<string>() : expectedRemovedPipe.Split('|');
            if (expectedRemoved.Length == 0)
            {
                Assert.Equal("0", bag["Parts.Length"]); // no removed matches
                Assert.Equal("false", bag["Parts.Valid"]); // valid false
            }
            else
            {
                for (int i = 0; i < expectedRemoved.Length; i++)
                {
                    var key = $"Parts[{i}]";
                    Assert.True(bag.ContainsKey(key));
                    Assert.Equal(expectedRemoved[i], bag[key]);
                }
                Assert.Equal(expectedRemoved.Length.ToString(CultureInfo.InvariantCulture), bag["Parts.Length"]);
                Assert.Equal("true", bag["Parts.Valid"]); // valid true
            }
        }

        [Fact]
        public void MappingAction_Performs_Lookup_With_CaseModes_And_AssignFlag()
        {
            var lookups = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["colors"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Red"] = "R",
                    ["BLUE"] = "B",
                    ["green"] = "G"
                }
            };

            // exact case mode should match exact key
            var bag = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["In"] = "Red" };
            var act = new MappingAction("In", "Out", false, lookups["colors"]);
            Assert.True(act.Execute(bag, new CellContext("A", "Red", CultureInfo.InvariantCulture, new Dictionary<string, object?>())));
            Assert.Equal("R", bag["Out"]);

            // upper case mode
            bag = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["In"] = "blue" };
            act = new MappingAction("In", "Out", false, lookups["colors"]);
            Assert.True(act.Execute(bag, new CellContext("A", "blue", CultureInfo.InvariantCulture, new Dictionary<string, object?>())));
            Assert.Equal("B", bag["Out"]);

            // lower case mode
            bag = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["In"] = "GREEN" };
            act = new MappingAction("In", "Out", false, lookups["colors"]);
            Assert.True(act.Execute(bag, new CellContext("A", "GREEN", CultureInfo.InvariantCulture, new Dictionary<string, object?>())));
            Assert.Equal("G", bag["Out"]);

            // assign:true writes to assign:Out
            bag = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["In"] = "Red" };
            act = new MappingAction("In", "Out",  true, lookups["colors"]);
            Assert.True(act.Execute(bag, new CellContext("A", "Red", CultureInfo.InvariantCulture, new Dictionary<string, object?>())));
            Assert.Equal("R", bag["assign:Out"]);

            // unknown table -> false
            bag = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["In"] = "Red" };
            act = new MappingAction("In", "Out", false, lookups["unknown"]);
            Assert.False(act.Execute(bag, new CellContext("A", "Red", CultureInfo.InvariantCulture, new Dictionary<string, object?>())));

            // no input -> false
            bag = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["In"] = "" };
            act = new MappingAction("In", "Out", false, lookups["colors"]);
            Assert.False(act.Execute(bag, new CellContext("A", "", CultureInfo.InvariantCulture, new Dictionary<string, object?>())));
        }

        [Fact]
        public void ActionsFactory_Creates_Correct_Action_Types_And_Validates_Parameters()
        {
            var lookups = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["t"] = new Dictionary<string, string>()
            };

            // assign
            var cfg = new ActionConfig { Op = "assign", Input = "A", Output = "B" };
            var a = ActionsFactory.Create(cfg, lookups);
            Assert.IsType<SimpleAssignAction>(a);

            // conditional missing param throws
            var cfg2 = new ActionConfig { Op = "conditional", Input = "A", Output = "B" };
            Assert.Throws<ArgumentException>(() => ActionsFactory.Create(cfg2, lookups));


            // find
            var cfg4 = new ActionConfig { Op = "find", Input = "A", Output = "B", Parameters = new Dictionary<string, string> { ["pattern"] = "\\d+", ["options"] = "all,remove" } };
            var a4 = ActionsFactory.Create(cfg4, lookups);
            Assert.IsType<FindAction>(a4);

            // split
            var cfg5 = new ActionConfig { Op = "split", Input = "A", Output = "B", Parameters = new Dictionary<string, string> { ["delimiter"] = "," } };
            var a5 = ActionsFactory.Create(cfg5, lookups);
            Assert.IsType<SplitAction>(a5);

            // map missing table throws
            var cfg6 = new ActionConfig { Op = "map", Input = "A", Output = "B" };
            Assert.Throws<ArgumentException>(() => ActionsFactory.Create(cfg6, lookups));

            // map ok
            var cfg7 = new ActionConfig { Op = "map", Input = "A", Output = "B", Parameters = new Dictionary<string, string> { ["table"] = "t" } };
            var a7 = ActionsFactory.Create(cfg7, lookups);
            Assert.IsType<MappingAction>(a7);

            // unknown op -> noop
            var cfg8 = new ActionConfig { Op = "unknown", Input = "A", Output = "B" };
            var a8 = ActionsFactory.Create(cfg8, lookups);
            Assert.Equal("noop", a8.Op);
        }

        [Fact]
        public void ActionHelpers_WriteListOutput_Handles_Null_And_Appends_Clean()
        {
            var bag = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            ActionHelpers.WriteListOutput(bag, "X", "cleaned", new List<string> { "one", "two" }, true, false);
            Assert.Equal("cleaned", bag["X.Clean"]);
            Assert.Equal("2", bag["X.Length"]);
            Assert.Equal("true", bag["X.Valid"]);
            Assert.Equal("one", bag["assign:X[0]"]);
            Assert.Equal("two", bag["assign:X[1]"]);

            // empty results
            var bag2 = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            ActionHelpers.WriteListOutput(bag2, "Y", string.Empty, null, false, false);
            Assert.Equal(string.Empty, bag2["Y.Clean"]);
            Assert.Equal("0", bag2["Y.Length"]);
            Assert.Equal("false", bag2["Y.Valid"]);
        }
    }
}
