using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis;
using JetBrains.ReSharper.Psi;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Intentions.QuickFixes
{
    [TestUnity]
    public class PerformanceAnalysisDisableAvailabilityTests : QuickFixAfterSwaAvailabilityTestBase
    {
        protected override string RelativeTestDataPath => @"CSharp\Intentions\QuickFixes\PerformanceAnalysisDisable\Availability";

        [Test] public void Everything() { DoNamedTest(); }

        protected override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile psiSourceFile,
            IContextBoundSettingsStore boundSettingsStore)
        {
            return highlighting is UnityPerformanceInvocationWarning;
        }
    }

    [TestUnity]
    public class PerformanceAnalysisDisableQuickFixTests: CSharpQuickFixAfterSwaTestBase<PerformanceAnalysisDisableByCommentQuickFix>
    {
        protected override string RelativeTestDataPath => @"CSharp\Intentions\QuickFixes\PerformanceAnalysisDisable\";
        
        [Test] public void TestSimple1() { DoNamedTest(); }
        [Test] public void TestSimple2() { DoNamedTest(); }
        [Test] public void TestSimple3() { DoNamedTest(); }
    }
}