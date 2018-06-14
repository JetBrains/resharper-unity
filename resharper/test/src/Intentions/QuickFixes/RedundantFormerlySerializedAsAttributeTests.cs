using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.Feature.Services.QuickFixes;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Intentions.QuickFixes
{
    [TestUnity]
    public class RedundantFormerlySerializedAsAttributeQuickFixAvailabilityTests : QuickFixAvailabilityTestBase
    {
        protected override string RelativeTestDataPath=> @"Intentions\QuickFixes\RedundantFormerlySerializedAsAttribute\Availability";

        [Test] public void Test01() { DoNamedTest(); }
        [Test] public void Test02() { DoNamedTest(); }
    }

    [TestUnity]
    public class RedundantFormerlySerializedAsAttributeQuickFixRemoveTests : CSharpQuickFixTestBase<RedundantAttributeDeadCodeQuickFix>
    {
        protected override string RelativeTestDataPath=> @"Intentions\QuickFixes\RedundantFormerlySerializedAsAttribute";

        [Test] public void Test01() { DoNamedTest(); }
        [Test] public void Test02() { DoNamedTest(); }
    }
}