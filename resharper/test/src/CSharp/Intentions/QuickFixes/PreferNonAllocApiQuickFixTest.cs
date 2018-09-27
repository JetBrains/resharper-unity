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
        
        [Test] public void NotAvailableDueToUnsupportedConstructionTest01() { DoNamedTest(); }
        [Test] public void NotAvailableDueToUnsupportedConstructionTest02() { DoNamedTest(); }
        [Test] public void NotAvailableDueToUnsupportedConstructionTest03() { DoNamedTest(); }
        [Test] public void NotAvailableDueToUnsupportedConstructionTest04() { DoNamedTest(); }


    }
    
    [TestUnity]
    public class PreferNonAllocApiQuickFixTest : QuickFixTestBase<PreferNonAllocApiQuickFix>
    {
        protected override string RelativeTestDataPath => @"CSharp\Intentions\QuickFixes\PreferNonAllocApi";
        protected override bool AllowHighlightingOverlap => true;

        [Test] public void BasicTest01() { DoNamedTest(); }
        [Test] public void BasicTest02() { DoNamedTest(); }
        [Test] public void SplitDeclarationTest01() { DoNamedTest(); }
        [Test] public void SplitDeclarationTest02() { DoNamedTest(); }
        [Test] public void SplitDeclarationTest03() { DoNamedTest(); }
        [Test] public void SplitDeclarationTest04() { DoNamedTest(); }
        [Test] public void ExpressionStatementTest() { DoNamedTest(); }
        [Test] public void PositionalArgumentsTest01() { DoNamedTest(); }
        [Test] public void PositionalArgumentsTest02() { DoNamedTest(); }
        [Test] public void PositionalArgumentsTest03() { DoNamedTest(); }
        [Test] public void UniqueNameTest() { DoNamedTest(); }
        
    }
}