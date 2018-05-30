using JetBrains.ReSharper.FeaturesTestFramework.ContextHighlighters;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Json.Daemon.ContextHighlighters
{
    [TestUnity]
    [TestFileExtension(".asmdef")]
    public class AsmDefUsageContextHighlighterTests : ContextHighlighterTestBase
    {
        protected override string RelativeTestDataPath => @"Json\" + base.RelativeTestDataPath;
        protected override string ExtraPath => @"AsmDefReferences";

        [Test] public void Test01() { DoNamedTest(); }
        [Test] public void Test02() { DoNamedTest(); }
        [Test] public void Test03() { DoNamedTest(); }
        [Test] public void Test04() { DoNamedTest(); }
    }
}