using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Tests.TestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.MoveQuickFixes;
using JetBrains.ReSharper.Psi;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Intentions.QuickFixes
{
    [TestUnity]
    public class MoveNullComparisonQuickFixAvailabilityTests : QuickFixAvailabilityTestBase
    {
        protected override string RelativeTestDataPath=> @"CSharp\Intentions\QuickFixes\MoveNullComparison\Availability";

        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Move null comparison")] public void EveryThingAvailable() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Move null comparison")] public void NotAvailableDueToLocalDependencies1() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Move null comparison")] public void NotAvailableDueToLocalDependencies2() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Move null comparison")]  public void NotAvailableDueToMissedTypeArgument() {DoNamedTest(); }

        protected override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile psiSourceFile,
            IContextBoundSettingsStore boundSettingsStore)
        {
            IHighlightingTestBehaviour? highlightingTestBehaviour = highlighting as IHighlightingTestBehaviour;
            return (highlightingTestBehaviour == null || !highlightingTestBehaviour.IsSuppressed) && highlighting is UnityPerformanceNullComparisonWarning;
        }
    }


    [TestUnity]
    public class MoveNullComparisonQuickFixTests : CSharpQuickFixAfterSwaTestBase<MoveNullComparisonQuickFix>
    {
        protected override string RelativeTestDataPath=> @"CSharp\Intentions\QuickFixes\MoveNullComparison";

        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Move null comparison")] public void MoveToStart() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Move null comparison")] public void MoveToAwake() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Move null comparison")] public void MoveOutsideTheLoop() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Move null comparison")] public void CorrectNameGeneration() {DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Move null comparison")] public void CorrectNameGeneration1() {DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Move null comparison")] public void CorrectNameGeneration2() {DoNamedTest(); }
    }
}