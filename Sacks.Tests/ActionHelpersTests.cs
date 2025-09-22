using System;
using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace ParsingEngine.Tests
{
    public class ActionHelpersTests
    {
        [Fact]
        public void WriteListOutput_Appends_Clean_When_Requested()
        {
            var bag = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            ActionHelpers.WriteListOutput(bag, "X", "cleaned", new List<string> { "one", "two" }, true,false);
            Assert.Equal("cleaned", bag["X.Clean"]);
            Assert.Equal("2", bag["X.Length"]);
            Assert.Equal("true", bag["X.Valid"]);
            Assert.Equal("one", bag["X[0]"]);
            Assert.Equal("two", bag["X[1]"]);
        }
    }
}
