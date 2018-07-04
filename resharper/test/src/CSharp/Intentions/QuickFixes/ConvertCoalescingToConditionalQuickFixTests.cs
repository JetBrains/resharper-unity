using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Intentions.QuickFixes
{
    [TestUnity]
    public class ConvertCoalescingToConditionalQuickFixAvailabilityTest : QuickFixAvailabilityTestBase
    {
        protected override string RelativeTestDataPath => @"CSharp\Intentions\QuickFixes\ConvertCoalescingToConditional\Availability";

        [Test] public void Test01() { DoNamedTest(); }
        [Test] public void Test02() { DoNamedTest(); }
        [Test] public void Test03() { DoNamedTest(); }
        [Test] public void Test04() { DoNamedTest(); }
    }

    [TestUnity]
    public class ConvertCoalescingToConditionalQuickFixTests : QuickFixTestBase<ConvertCoalescingToConditionalQuickFix>
    {
        protected override string RelativeTestDataPath => @"CSharp\Intentions\QuickFixes\ConvertCoalescingToConditional";
        protected override bool AllowHighlightingOverlap => true;

        [Test] public void Test01() { DoNamedTest(); }
        [Test] public void Test02() { DoNamedTest(); }
        [Test] public void Test03() { DoNamedTest(); }
        [Test] public void Test04() { DoNamedTest(); }
    }
}
