using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Intentions.QuickFixes
{
    [TestUnity]
    [TestCustomInspectionSeverity(UnityObjectNullCoalescingWarning.HIGHLIGHTING_ID, Severity.WARNING)]
    public class ConvertCoalescingToConditionalQuickFixAvailabilityTest : QuickFixAvailabilityTestBase
    {
        protected override string RelativeTestDataPath => @"CSharp\Intentions\QuickFixes\ConvertCoalescingToConditional\Availability";

        [Test, UnityPluginBackendChecklist("Quick fixes", "Unity object checks", "Convert coalescing to conditional")] public void Test01() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Unity object checks", "Convert coalescing to conditional")] public void Test02() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Unity object checks", "Convert coalescing to conditional")] public void Test03() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Unity object checks", "Convert coalescing to conditional")] public void Test04() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Unity object checks", "Convert coalescing to conditional")] public void Test05() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Unity object checks", "Convert coalescing to conditional")] public void Test06() { DoNamedTest(); }
    }

    [TestUnity]
    [TestCustomInspectionSeverity(UnityObjectNullCoalescingWarning.HIGHLIGHTING_ID, Severity.WARNING)]
    public class ConvertCoalescingToConditionalQuickFixTests : QuickFixTestBase<ConvertCoalescingToConditionalQuickFix>
    {
        protected override string RelativeTestDataPath => @"CSharp\Intentions\QuickFixes\ConvertCoalescingToConditional";
        protected override bool AllowHighlightingOverlap => true;

        [Test, UnityPluginBackendChecklist("Quick fixes", "Unity object checks", "Convert coalescing to conditional")] public void Test01() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Unity object checks", "Convert coalescing to conditional")] public void Test02() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Unity object checks", "Convert coalescing to conditional")] public void Test03() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Unity object checks", "Convert coalescing to conditional")] public void Test04() { DoNamedTest(); }
        [Test, ExecuteScopedActionInFile, UnityPluginBackendChecklist("Quick fixes", "Unity object checks", "Convert coalescing to conditional")] public void Test05() { DoNamedTest(); }
        [Test, ExecuteScopedActionInFile, UnityPluginBackendChecklist("Quick fixes", "Unity object checks", "Convert coalescing to conditional")] public void Test06() { DoNamedTest(); }
    }
}
