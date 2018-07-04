using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Intentions.QuickFixes
{
    [TestUnity]
    public class RedundantHideInInspectorAttributeQuickFixAvailabilityTests : QuickFixAvailabilityTestBase
    {
        protected override string RelativeTestDataPath=> @"Intentions\QuickFixes\RedundantHideInInspectorAttribute\Availability";

        [Test] public void Test01() { DoNamedTest(); }
        [Test] public void Test02() { DoNamedTest(); }
    }

    [TestUnity]
    public class RedundantHideInInspectorAttributeQuickFixTests : CSharpQuickFixTestBase<RedundantAttributeDeadCodeQuickFix>
    {
        protected override string RelativeTestDataPath=> @"Intentions\QuickFixes\RedundantHideInInspectorAttribute";

        [Test] public void Test01() { DoNamedTest(); }
        [Test] public void Test02() { DoNamedTest(); }
    }
}