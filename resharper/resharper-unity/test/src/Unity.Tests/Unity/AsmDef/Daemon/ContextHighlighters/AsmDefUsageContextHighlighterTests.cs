using JetBrains.ReSharper.FeaturesTestFramework.ContextHighlighters;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.AsmDef.Daemon.ContextHighlighters
{
    [TestUnity]
    [TestFileExtension(".asmdef")]
    public class AsmDefUsageContextHighlighterTests : ContextHighlighterTestBase
    {
        protected override string RelativeTestDataPath => @"AsmDef\" + base.RelativeTestDataPath;
        protected override string ExtraPath => @"AsmDefReferences";

        [Test] public void Test01() { DoNamedTest(); }
        [Test] public void Test02() { DoNamedTest(); }
        [Test] public void Test03() { DoNamedTest(); }
        [Test] public void Test04() { DoNamedTest(); }

        [Test]
        public void TestGuidReference01()
        {
          DoTestSolution(["GuidReference01.asmdef"], ["GuidReference_SecondProject.asmdef", "GuidReference_SecondProject.asmdef.meta"]);
        }

        [Test]
        public void TestAsmRef()
        {
            DoTestSolution("AsmRefReference01.asmref", "AsmRefDefinition01.asmdef");
        }

        [Test]
        public void TestAsmRefGuid()
        {
            DoTestSolution("AsmRefGuidReference01.asmref", "AsmRefDefinition01.asmdef", "AsmRefDefinition01.asmdef.meta");
        }
    }
}