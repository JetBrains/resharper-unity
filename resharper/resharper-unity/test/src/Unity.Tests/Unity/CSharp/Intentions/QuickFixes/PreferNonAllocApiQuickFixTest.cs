using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Intentions.QuickFixes
{
    [TestUnity]
    public class PreferNonAllocApiAvailabilityTest : QuickFixAvailabilityTestBase
    {
        protected override string RelativeTestDataPath => @"CSharp\Intentions\QuickFixes\PreferNonAllocApi\Availability";

        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer non-allocating API")] public void AvailableTest01() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer non-allocating API")] public void AvailableTest02() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer non-allocating API")] public void AvailableTest03() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer non-allocating API")] public void AvailablePrePreprocessorDirectivesTest() { DoNamedTest(); }

        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer non-allocating API")] public void NotAvailableDueToIncorrectSignatureTest01() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer non-allocating API")] public void NotAvailableDueToIncorrectSignatureTest02() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer non-allocating API")] public void NotAvailableDueToNoNonAllocTest01() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer non-allocating API")] public void NotAvailableDueToNoNonAllocTest02() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer non-allocating API")] public void NotAvailableDueToWrongMethodNameTest() { DoNamedTest(); }

        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer non-allocating API")] public void NotAvailableDueToUnsupportedConstructionTest01() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer non-allocating API")] public void NotAvailableDueToUnsupportedConstructionTest02() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer non-allocating API")] public void NotAvailableDueToUnsupportedConstructionTest03() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer non-allocating API")] public void NotAvailableDueToUnsupportedConstructionTest04() { DoNamedTest(); }
    }

    [TestUnity]
    public class PreferNonAllocApiQuickFixTest : QuickFixTestBase<PreferNonAllocApiQuickFix>
    {
        protected override string RelativeTestDataPath => @"CSharp\Intentions\QuickFixes\PreferNonAllocApi";
        protected override bool AllowHighlightingOverlap => true;

        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer non-allocating API")] public void BasicTest01() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer non-allocating API")] public void BasicTest02() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer non-allocating API")] public void SplitDeclarationTest01() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer non-allocating API")] public void SplitDeclarationTest02() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer non-allocating API")] public void SplitDeclarationTest03() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer non-allocating API")] public void SplitDeclarationTest04() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer non-allocating API")] public void ExpressionStatementTest() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer non-allocating API")] public void PositionalArgumentsTest01() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer non-allocating API")] public void PositionalArgumentsTest02() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer non-allocating API")] public void PositionalArgumentsTest03() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer non-allocating API")] public void UniqueNameTest() { DoNamedTest(); }
    }
}