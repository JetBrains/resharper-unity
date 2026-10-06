using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Intentions.QuickFixes
{
    [TestUnity]
    public class ConvertToCompareTagQuickFixAvailabilityTest : QuickFixAvailabilityTestBase
    {
        protected override string RelativeTestDataPath => @"CSharp\Intentions\QuickFixes\ConvertToCompareTag\Availability";

        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Convert to CompareTag")] public void Test01() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Convert to CompareTag")] public void Test02() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Convert to CompareTag")] public void Test03() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Convert to CompareTag")] public void Test04() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Convert to CompareTag")] public void Test05() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Convert to CompareTag")] public void Test06() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Convert to CompareTag")] public void Test07() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Convert to CompareTag")] public void Test08() { DoNamedTest(); }
    }

    [TestUnity]
    public class ConvertToCompareTagQuickFixTest : QuickFixTestBase<ConvertToCompareTagQuickFix>
    {
        protected override string RelativeTestDataPath => @"CSharp\Intentions\QuickFixes\ConvertToCompareTag";
        protected override bool AllowHighlightingOverlap => true;

        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Convert to CompareTag")] public void Test01() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Convert to CompareTag")] public void Test02() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Convert to CompareTag")] public void Test03() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Convert to CompareTag")] public void Test04() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Convert to CompareTag")] public void Test05() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Convert to CompareTag")] public void Test06() { DoNamedTest(); }
        [Test, ExecuteScopedActionInFile, UnityPluginBackendChecklist("Quick fixes", "Performance", "Convert to CompareTag")] public void Test07() { DoNamedTest(); }
        [Test, ExecuteScopedActionInFile, UnityPluginBackendChecklist("Quick fixes", "Performance", "Convert to CompareTag")] public void Test08() { DoNamedTest(); }
    }
}