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

        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Move costly method invocation")]  public void EveryThingAvailable() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Move costly method invocation")][Ignore("AvailabilityTestBase does not support global analysis")]  public void NotAvailableDueToLocalDependencies1() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Move costly method invocation")][Ignore("AvailabilityTestBase does not support global analysis")]  public void NotAvailableDueToLocalDependencies2() { DoNamedTest(); }

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

        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Move costly method invocation")] public void MoveToStart() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Move costly method invocation")] public void MoveToAwake() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Move costly method invocation")] public void MoveOutsideTheLoop() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Move costly method invocation")] public void MoveOutsideTheLoop2() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Move costly method invocation")] public void MoveOutsideTheLoop3() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Move costly method invocation")] public void MoveOutsideTheLoop4() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Move costly method invocation")] public void FieldGenerationWithRespectToCodeStyleTest() {DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Move costly method invocation")] public void MultiReplace() { DoNamedTest();}
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Move costly method invocation")] public void MoveCostlyVoid() {DoNamedTest();}
    }
}