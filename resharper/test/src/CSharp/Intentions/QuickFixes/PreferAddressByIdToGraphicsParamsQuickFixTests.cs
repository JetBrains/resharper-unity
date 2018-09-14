using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Intentions.QuickFixes
{
    [TestUnity]
    public class PreferAddressByIdToGraphicsParamsQuickFixTests : QuickFixTestBase<PreferAddressByIdToGraphicsParamsQuickFix>
    {
        protected override string RelativeTestDataPath => @"CSharp\Intentions\QuickFixes\PreferAddressByIdToGraphicsParams";
        protected override bool AllowHighlightingOverlap => true;

        [Test] public void SimpleTest() { DoNamedTest(); }
        [Test] public void NewNameTest() { DoNamedTest(); }
        [Test] public void ReuseTest() { DoNamedTest(); }
        [Test] public void ReuseFailedCreateNewTest() { DoNamedTest(); }
    }
}