using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Intentions.QuickFixes
{
    [TestUnity]
    public class UseExplicitTypeInsteadOfStringQuickFixAvailabilityTest : QuickFixAvailabilityTestBase
    {
        protected override string RelativeTestDataPath => @"CSharp\Intentions\QuickFixes\UseExplicitTypeInsteadOfString\Availability";

        [Test] public void Test01() { DoNamedTest(); }
        [Test] public void Test02() { DoNamedTest(); }
        [Test] public void Test03() { DoNamedTest(); }
        [Test] public void Test04() { DoNamedTest(); }
        [Test] public void Test05() { DoNamedTest(); }
        [Test] public void Test06() { DoNamedTest(); }
    }
    
    [TestUnity]
    public class UseExplicitTypeInsteadOfStringQuickFixTest : QuickFixTestBase<UseExplicitTypeInsteadOfStringQuickFix>
    {
        protected override string RelativeTestDataPath => @"CSharp\Intentions\QuickFixes\UseExplicitTypeInsteadOfString";
        protected override bool AllowHighlightingOverlap => true;

        [Test] public void Test01() { DoNamedTest(); }
        [Test] public void Test02() { DoNamedTest(); } 
        [Test] public void Test03() { DoNamedTest(); }
        [Test] public void Test04() { DoNamedTest(); }
        [Test] public void Test05() { DoNamedTest(); }
        [Test] public void Test06() { DoNamedTest(); }
        [Test] public void Test07() { DoNamedTest(); }
        [Test] public void Test08() { DoNamedTest(); }
        [Test] public void Test09() { DoNamedTest(); }
        
    }
}