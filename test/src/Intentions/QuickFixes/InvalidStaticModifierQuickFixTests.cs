using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.Feature.Services.QuickFixes;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Intentions.QuickFixes
{
    [TestUnity]
    public class InvalidStaticModifierQuickFixAvailabilityTests : 
        QuickFixAvailabilityTestBase
    {
        protected override string RelativeTestDataPath=> @"Intentions\QuickFixes\InvalidStaticModifier\Availability";

        [Test] public void Test01() { DoNamedTest(); }
        [Test] public void Test02() { DoNamedTest(); }
    }

    [TestUnity]
    public class InvalidStaticModifierQuickFixTests : CSharpQuickFixTestBase<InvalidStaticModifierQuickFix>
    {
        protected override string RelativeTestDataPath=> @"Intentions\QuickFixes\InvalidStaticModifier";

        [Test] public void Test01() { DoNamedTest(); }
        [Test] public void Test02() { DoNamedTest(); }
    }
}