using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Intentions.QuickFixes.UsageChecking;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Intentions.QuickFixes
{
    [TestUnity]
    public class RedundantEventFunctionQuickFixAvailabilityTests : QuickFixAvailabilityTestBase
    {
        protected override string RelativeTestDataPath => @"CSharp\Intentions\QuickFixes\RedundantEventFunction\Availability";

        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Remove redundant event function")] public void Test01() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Remove redundant event function")] public void Test02() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Remove redundant event function")] public void Test03() { DoNamedTest(); }
    }

    [TestUnity]
    public class RedundantEventFunctionQuickFixTests : CSharpQuickFixTestBase<RemoveUnusedElementFix>
    {
        protected override string RelativeTestDataPath => @"CSharp\Intentions\QuickFixes\RedundantEventFunction";

        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Remove redundant event function")] public void Test01() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Remove redundant event function")] public void Test02() { DoNamedTest(); }
    }
}