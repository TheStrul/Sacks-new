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
            var pb = new PropertyBag();
            pb.SetVariable("Src", input);
            var act = new SimpleAssignAction("Src", "Dst");
            var ok = act.Execute(new CellContext("A", input, CultureInfo.InvariantCulture, pb));
            Assert.True(ok);
            Assert.True(pb.Variables.ContainsKey("Dst[0]"));
            Assert.Equal(expected, pb.Variables["Dst[0]"]); // result stored starting at index 0
            Assert.Equal("1", pb.Variables["Dst.Length"]); // length
            Assert.Equal("true", pb.Variables["Dst.Valid"]); // valid
        }

        [Theory]
        [InlineData(":", new[] { "a", "b", "c" }, 3)]
        [InlineData(",", new[] { "one", "two" }, 2)]
        [InlineData("|", new string[] { }, 0)]
        public void SplitAction_Splits_Correctly(string delimiter, string[] parts, int expectedCount)
        {
            parts ??= Array.Empty<string>();
            var input = string.Join(delimiter, parts);
            var pb = new PropertyBag();
            pb.SetVariable("Src", input);
            var act = new SplitAction("Src", "Parts", delimiter);
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
                for (int i = 0; i < expectedCount; i++)
                {
                    Assert.Equal(parts[i], pb.Variables[$"Parts[{i}]"]);
                }
            }
        }


        [Fact]
        public void FindAndRemoveAction_Cleans_And_Lists_Removed()
        {
            var input = "abc123def45";
            var pb = new PropertyBag();
            pb.SetVariable("Src", input);

            // Use FindAction to remove all digit sequences
            var act = new FindAction("Src", "Parts", "\\d+", new List<string>() { "all", "remove" }, false, null);
            var ok = act.Execute(new CellContext("A", input, CultureInfo.InvariantCulture, pb));
            Assert.True(ok);
            // cleaned value is at Parts.Clean
            Assert.Equal("abcdef", pb.Variables["Parts.Clean"]);
            // removed matches are at Parts[0], Parts[1]
            Assert.Equal("123", pb.Variables["Parts[0]"]);
            Assert.Equal("45", pb.Variables["Parts[1]"]);
            // length should be 2
            Assert.Equal("2", pb.Variables["Parts.Length"]);
            Assert.Equal("true", pb.Variables["Parts.Valid"]); // valid
        }

        [Theory]
        [InlineData("Acme UPTOWN BRAND Co", "Acme Co", "UPTOWN|BRAND")]
        [InlineData("No Capitals here", "No Capitals here", "")]
        public void FindAndRemoveAction_Removes_All_Capital_Words(string input, string expectedCleanNormalized, string expectedRemovedPipe)
        {
            var pb = new PropertyBag();
            pb.SetVariable("Src", input);

            // Pattern: Unicode uppercase words, remove all
            var act = new FindAction("Src", "Parts", "\\b\\p{Lu}+\\b", new List<string>() { "all", "remove" }, false, null);
            var ok = act.Execute(new CellContext("A", input, CultureInfo.InvariantCulture, pb));
            Assert.True(pb.Variables.ContainsKey("Parts.Clean"));
            var actualClean = pb.Variables["Parts.Clean"] ?? string.Empty;
            // Normalize whitespace for comparison
            var normActual = Regex.Replace(actualClean, "\\s+", " ").Trim();
            Assert.Equal(expectedCleanNormalized, normActual);

            var expectedRemoved = string.IsNullOrEmpty(expectedRemovedPipe) ? Array.Empty<string>() : expectedRemovedPipe.Split('|');
            if (expectedRemoved.Length == 0)
            {
                Assert.Equal("0", pb.Variables["Parts.Length"]); // no removed matches
                Assert.Equal("false", pb.Variables["Parts.Valid"]); // valid false
            }
            else
            {
                for (int i = 0; i < expectedRemoved.Length; i++)
                {
                    var key = $"Parts[{i}]";
                    Assert.True(pb.Variables.ContainsKey(key));
                    Assert.Equal(expectedRemoved[i], pb.Variables[key]);
                }
                Assert.Equal(expectedRemoved.Length.ToString(CultureInfo.InvariantCulture), pb.Variables["Parts.Length"]);
                Assert.Equal("true", pb.Variables["Parts.Valid"]); // valid true
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
            var pb = new PropertyBag();
            pb.SetVariable("In", "Red");
            var act = new MappingAction("In", "Out", false, lookups["colors"]);
            Assert.True(act.Execute(new CellContext("A", "Red", CultureInfo.InvariantCulture, pb)));
            Assert.Equal("R", pb.Variables["Out"]);

            // upper case mode
            pb = new PropertyBag();
            pb.SetVariable("In", "blue" );
            act = new MappingAction("In", "Out", false, lookups["colors"]);
            Assert.True(act.Execute(new CellContext("A", "blue", CultureInfo.InvariantCulture, pb)));
            Assert.Equal("B", pb.Variables["Out"]);

            // lower case mode
            pb = new PropertyBag();
            pb.SetVariable("In", "GREEN");
            act = new MappingAction("In", "Out", false, lookups["colors"]);
            Assert.True(act.Execute(new CellContext("A", "GREEN", CultureInfo.InvariantCulture, pb)));
            Assert.Equal("G", pb.Variables["Out"]);

            // assign:true writes to assign:Out
            pb = new PropertyBag();
            pb.SetVariable("In", "Red");
            act = new MappingAction("In", "Out", true, lookups["colors"]);
            Assert.True(act.Execute(new CellContext("A", "Red", CultureInfo.InvariantCulture, pb)));
            Assert.Equal("R", pb.Variables["assign:Out"]);

            // unknown table -> false
            pb = new PropertyBag();
            pb.SetVariable("In", "Red");
            act = new MappingAction("In", "Out", false, new Dictionary<string, string>());
            Assert.False(act.Execute(new CellContext("A", "Red", CultureInfo.InvariantCulture, pb)));

            // no input -> false
            pb = new PropertyBag();
            pb.SetVariable("In", "");
            act = new MappingAction("In", "Out", false, lookups["colors"]);
            Assert.False(act.Execute(new CellContext("A", "", CultureInfo.InvariantCulture, pb)));
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

    }
}
