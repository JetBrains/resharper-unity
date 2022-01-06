using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.Json.Psi;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Json.Psi.Parsing
{
    [TestFileExtension(".json")]
    public class ParserTests : ParserTestBase<JsonNewLanguage>
    {
        protected override string RelativeTestDataPath => @"Json\Psi\Parsing";

        [TestCase("everything")]
        [TestCase("empty")]
        [TestCase("emptyArray")]
        [TestCase("emptyObject")]
        [TestCase("boolLiteral")]
        [TestCase("stringLiteral")]
        [TestCase("literal")]
        [TestCase("array")]
        [TestCase("object")]
        [TestCase("object1")]
        [TestCase("object2")]
        [TestCase("object3")]
        [TestCase("object4")]
        [TestCase("object5")]
        [TestCase("object6")]
        public void TestParser(string name) => DoOneTest(name);

        protected override IList<string> TraceCategories()
        {
            return new List<string>() {"JetBrains.Application"};
        }
    }
}
