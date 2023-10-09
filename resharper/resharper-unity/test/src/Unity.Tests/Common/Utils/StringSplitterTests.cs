using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.Unity.Common.Utils;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Common.Utils
{
    public class StringSplitterTests
    {
        [TestCase("", new string[0])]
        [TestCase(" ", new string[0])]
        [TestCase("  ", new string[0])]
        [TestCase("   ", new string[0])]
        [TestCase("a", new[] { "a" })]
        [TestCase(" a", new[] { "a" })]
        [TestCase(" a ", new[] { "a" })]
        [TestCase("a b", new[] { "a", "b" })]
        [TestCase("a b ", new[] { "a", "b" })]
        [TestCase(" a b", new[] { "a", "b" })]
        [TestCase(" a b ", new[] { "a", "b" })]
        [TestCase(" a   b ", new[] { "a", "b" })]
        [TestCase(" a\tb\tc", new[] { "a", "b", "c" })]
        public void TestSplitByWhitespace(string input, string[] slices)
        {
            var splitter = StringSplitter.ByWhitespace(input);
            var sliceList = new List<string>();
            while (splitter.TryGetNextSlice(out var slice)) 
                sliceList.Add(slice.ToString());
            CollectionAssert.AreEqual(slices, sliceList);
        }
    }
}