using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.MoveQuickFixes;
using JetBrains.ReSharper.Psi;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Intentions.QuickFixes
{
    [TestUnity]
    public class MoveCostlyMethodQuickFixAvailabilityTests : QuickFixAvailabilityTestBase
    {
        protected override string RelativeTestDataPath=> @"CSharp\Intentions\QuickFixes\MoveCostlyMethod\Availability";

        [Test] public void EveryThingAvailable() { DoNamedTest(); }
        [Test] public void NotAvailableDueToLocalDependencies1() { DoNamedTest(); }
        [Test] public void NotAvailableDueToLocalDependencies2() { DoNamedTest(); }

        protected override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile psiSourceFile,
            IContextBoundSettingsStore boundSettingsStore)
        {
            HighlightingSettingsManager instance = HighlightingSettingsManager.Instance;
            IHighlightingTestBehaviour highlightingTestBehaviour = highlighting as IHighlightingTestBehaviour;
            return (highlightingTestBehaviour == null || !highlightingTestBehaviour.IsSuppressed) && highlighting is PerformanceCriticalCodeHighlightingBase;
        }
    }

    
    [TestUnity]
    public class MoveCostlyMethodQuickFixTests : CSharpQuickFixTestBase<MoveCostlyInvocationtQuickFix>
    {
        protected override string RelativeTestDataPath=> @"CSharp\Intentions\QuickFixes\MoveCostlyMethod";

        [Test] public void MoveToStart() { DoNamedTest(); }
        [Test] public void MoveToAwake() { DoNamedTest(); }
        [Test] public void MoveOutsideTheLoop() { DoNamedTest(); }
    }
}