using JetBrains.ReSharper.Plugins.Json.Psi;
using JetBrains.ReSharper.TestFramework;
using JetBrains.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Json.Psi.Parsing
{
    [TestFileExtension(".json")]
    [TestTraceCategories("JetBrains.Application")]
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
    }
}
