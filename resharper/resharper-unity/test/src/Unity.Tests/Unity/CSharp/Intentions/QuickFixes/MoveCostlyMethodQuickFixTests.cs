using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Tests.TestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Highlightings;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.MoveQuickFixes;
using JetBrains.ReSharper.Psi;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Intentions.QuickFixes
{
    [TestUnity]
    public class MoveCostlyMethodQuickFixAvailabilityTests : QuickFixAvailabilityTestBase
    {
        protected override string RelativeTestDataPath=> @"CSharp\Intentions\QuickFixes\MoveCostlyMethod\Availability";

        [Test]  public void EveryThingAvailable() { DoNamedTest(); }
        [Test][Ignore("AvailabilityTestBase does not support global analysis")]  public void NotAvailableDueToLocalDependencies1() { DoNamedTest(); }
        [Test][Ignore("AvailabilityTestBase does not support global analysis")]  public void NotAvailableDueToLocalDependencies2() { DoNamedTest(); }

        protected override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile psiSourceFile,
            IContextBoundSettingsStore boundSettingsStore)
        {
            return (!(highlighting is IHighlightingTestBehaviour highlightingTestBehaviour) ||
                    !highlightingTestBehaviour.IsSuppressed) &&
                   highlighting is IUnityPerformanceHighlighting && !(highlighting is UnityPerformanceCriticalCodeLineMarker);
        }
    }


    [TestUnity]
    public class MoveCostlyMethodQuickFixTests : CSharpQuickFixAfterSwaTestBase<MoveCostlyInvocationQuickFix>
    {
        protected override string RelativeTestDataPath=> @"CSharp\Intentions\QuickFixes\MoveCostlyMethod";

        [Test] public void MoveToStart() { DoNamedTest(); }
        [Test] public void MoveToAwake() { DoNamedTest(); }
        [Test] public void MoveOutsideTheLoop() { DoNamedTest(); }
        [Test] public void MoveOutsideTheLoop2() { DoNamedTest(); }
        [Test] public void MoveOutsideTheLoop3() { DoNamedTest(); }
        [Test] public void MoveOutsideTheLoop4() { DoNamedTest(); }
        [Test] public void FieldGenerationWithRespectToCodeStyleTest() {DoNamedTest(); }
        [Test] public void MultiReplace() { DoNamedTest();}
        [Test] public void MoveCostlyVoid() {DoNamedTest();}
    }
}