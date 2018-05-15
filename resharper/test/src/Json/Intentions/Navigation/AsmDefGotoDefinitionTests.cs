using JetBrains.ReSharper.IntentionsTests.Navigation;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Json.Intentions.Navigation
{
    [TestUnity]
    [TestFileExtension(".asmdef")]
    public class AsmDefGotoDeclarationTests : AllNavigationProvidersTestBase
    {
        protected override string RelativeTestDataPath => @"Json\" + base.RelativeTestDataPath;
        protected override string ExtraPath => "Navigation";

        [Test] public void Test01() { DoNamedTest("Ref1.asmdef"); }
        [Test] public void Test02() { DoNamedTest("Ref1.asmdef"); }
        [Test] public void Test03() { DoNamedTest("Ref1.asmdef", "Ref2.asmdef"); }
    }
}