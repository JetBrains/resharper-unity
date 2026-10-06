using JetBrains.ReSharper.IntentionsTests.Navigation;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.AsmDef.Intentions.Navigation
{
    [TestUnity]
    [TestFileExtension(".asmdef")]
    public class AsmDefGotoDeclarationTests : AllNavigationProvidersTestBase
    {
        protected override string RelativeTestDataPath => @"AsmDef\" + base.RelativeTestDataPath;
        protected override string ExtraPath => "Navigation";

        [Test, UnityPluginBackendChecklist("Assembly definition files", "References", "Go to definition")] public void Test01() { DoNamedTest("Ref1.asmdef"); }
        [Test, UnityPluginBackendChecklist("Assembly definition files", "References", "Go to definition")] public void Test02() { DoNamedTest("Ref1.asmdef"); }
        [Test, UnityPluginBackendChecklist("Assembly definition files", "References", "Go to definition")] public void Test03() { DoNamedTest("Ref1.asmdef", "Ref2.asmdef"); }

        [Test, UnityPluginBackendChecklist("Assembly definition files", "References", "Go to definition")] public void TestGuidReference01() { DoTestSolution([TestName2], ["GuidReference_SecondProject.asmdef", "GuidReference_SecondProject.asmdef.meta"]); }

        [Test, TestFileExtension(".asmref"), UnityPluginBackendChecklist("Assembly definition files", "References", "Go to definition")] public void TestAsmRefNamedReference() { DoNamedTest2("AsmRef_FirstProject.asmdef"); }
        [Test, TestFileExtension(".asmref"), UnityPluginBackendChecklist("Assembly definition files", "References", "Go to definition")] public void TestAsmRefGuidReference() { DoNamedTest2("AsmRef_FirstProject.asmdef", "AsmRef_FirstProject.asmdef.meta"); }
    }
}