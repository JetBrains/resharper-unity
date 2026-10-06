using JetBrains.ReSharper.FeaturesTestFramework.Completion;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Psi.Naming.Dots
{
    [TestUnity]
    public class UnityDotsNamingSuggestionsAdviserTest : CodeCompletionTestBase

    {
        protected override CodeCompletionTestType TestType => CodeCompletionTestType.ModernList;

        protected override string RelativeTestDataPath => @"CSharp\Psi\Naming\Dots";
        [Test]
        [UnityPluginBackendChecklist("DOTS and Burst", "DOTS naming suggestions")]
        public void TestRefRwRoNaming01() { DoNamedTest2(); }

        [Test]
        [UnityPluginBackendChecklist("DOTS and Burst", "DOTS naming suggestions")]
        public void TestRefRwRoNaming02() { DoNamedTest2(); }
    }
}