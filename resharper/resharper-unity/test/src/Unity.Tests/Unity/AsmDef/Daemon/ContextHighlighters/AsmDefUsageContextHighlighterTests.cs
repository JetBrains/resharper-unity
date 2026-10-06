using JetBrains.ReSharper.FeaturesTestFramework.ContextHighlighters;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.AsmDef.Daemon.ContextHighlighters
{
    [TestUnity]
    [TestFileExtension(".asmdef")]
    public class AsmDefUsageContextHighlighterTests : ContextHighlighterTestBase
    {
        protected override bool InitDataPackagesInTestFixtureSetup => false;
        protected override string RelativeTestDataPath => @"AsmDef\" + base.RelativeTestDataPath;
        protected override string ExtraPath => @"AsmDefReferences";

        [Test, UnityPluginBackendChecklist("Assembly definition files", "References", "Usage context highlighting")] public void Test01() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Assembly definition files", "References", "Usage context highlighting")] public void Test02() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Assembly definition files", "References", "Usage context highlighting")] public void Test03() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Assembly definition files", "References", "Usage context highlighting")] public void Test04() { DoNamedTest(); }

        [Test]
        [UnityPluginBackendChecklist("Assembly definition files", "References", "Usage context highlighting")]
        public void TestGuidReference01()
        {
          DoTestSolution(["GuidReference01.asmdef"], ["GuidReference_SecondProject.asmdef", "GuidReference_SecondProject.asmdef.meta"]);
        }

        [Test]
        [UnityPluginBackendChecklist("Assembly definition files", "References", "Usage context highlighting")]
        public void TestAsmRef()
        {
            DoTestSolution("AsmRefReference01.asmref", "AsmRefDefinition01.asmdef");
        }

        [Test]
        [UnityPluginBackendChecklist("Assembly definition files", "References", "Usage context highlighting")]
        public void TestAsmRefGuid()
        {
            DoTestSolution("AsmRefGuidReference01.asmref", "AsmRefDefinition01.asmdef", "AsmRefDefinition01.asmdef.meta");
        }
    }
}