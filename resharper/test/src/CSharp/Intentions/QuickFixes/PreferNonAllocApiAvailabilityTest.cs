using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Intentions.QuickFixes
{
    [TestUnity]
    public class PreferNonAllocApiAvailabilityTest : QuickFixAvailabilityTestBase
    {
        protected override string RelativeTestDataPath => @"CSharp\Intentions\QuickFixes\PreferNonAllocApi\Availability";

        [Test] public void AvailableTest01() { DoNamedTest(); }
        [Test] public void AvailableTest02() { DoNamedTest(); }
        [Test] public void AvailableTest03() { DoNamedTest(); }
        [Test] public void AvailablePrePreprocessorDirectivesTest() { DoNamedTest(); }
        
        [Test] public void NotAvailableDueToIncorrectSignatureTest01() { DoNamedTest(); }
        [Test] public void NotAvailableDueToIncorrectSignatureTest02() { DoNamedTest(); }
        [Test] public void NotAvailableDueToNoNonAllocTest01() { DoNamedTest(); }
        [Test] public void NotAvailableDueToNoNonAllocTest02() { DoNamedTest(); }
        [Test] public void NotAvailableDueToWrongMethodNameTest() { DoNamedTest(); }

    }
}