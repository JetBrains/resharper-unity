using JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.Psi;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.Uxml.Psi.Parsing
{
    [TestUnity]
    [TestFileExtension(UxmlProjectFileType.UXML_EXTENSION)]
    public class UxmlParserTests : ParserTestBase<UxmlLanguage>
    {
        protected override string RelativeTestDataPath => @"Uxml\Psi\Parsing";

        [TestCase("MainMenu")]
        public void TestParser(string name) => DoOneTest(name);
    }
}
