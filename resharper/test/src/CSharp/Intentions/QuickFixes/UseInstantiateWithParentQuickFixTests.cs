using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Intentions.QuickFixes
{
    [TestUnity]
    public class UseInstantiateWithParentQuickFixTests : QuickFixTestBase<UseInstantiateWithParentQuickFix>
    {
        protected override string RelativeTestDataPath => @"CSharp\Intentions\QuickFixes\UseInstantiateWithParent";
        protected override bool AllowHighlightingOverlap => true;

        [Test] public void SimpleTest() { DoNamedTest(); }
        [Test] public void SetParentWithFalseTest() { DoNamedTest(); }
        [Test] public void SetParentWithTrueTest() { DoNamedTest(); }
        [Test] public void SetParentByPropertyTest() { DoNamedTest(); }
        [Test] public void SomethingBetweenTest() { DoNamedTest(); }

    }
}