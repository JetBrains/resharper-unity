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

        [Test] public void Test01() { DoNamedTest("Ref1.asmdef"); }
        [Test] public void Test02() { DoNamedTest("Ref1.asmdef"); }
        [Test] public void Test03() { DoNamedTest("Ref1.asmdef", "Ref2.asmdef"); }

        [Test] public void TestGuidReference01() { DoTestSolution([TestName2], ["GuidReference_SecondProject.asmdef", "GuidReference_SecondProject.asmdef.meta"]); }

        [Test, TestFileExtension(".asmref")] public void TestAsmRefNamedReference() { DoNamedTest2("AsmRef_FirstProject.asmdef"); }
        [Test, TestFileExtension(".asmref")] public void TestAsmRefGuidReference() { DoNamedTest2("AsmRef_FirstProject.asmdef", "AsmRef_FirstProject.asmdef.meta"); }
    }
}