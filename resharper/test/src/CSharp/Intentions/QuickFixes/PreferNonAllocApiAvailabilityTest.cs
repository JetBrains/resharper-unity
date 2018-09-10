using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Intentions.QuickFixes
{
    [TestUnity]
    public class PreferNonAllocApiAvailabilityTest : QuickFixAvailabilityTestBase
    {
        protected override string RelativeTestDataPath => @"CSharp\Intentions\QuickFixes\PreferNonAllocApi\Availability";

        [Test] public void NotAvailableDueToIncorrectSignatureTest() { DoNamedTest(); }
        [Test] public void Available01() { DoNamedTest(); }
    }
}