using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis;
using JetBrains.ReSharper.Psi;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis
{

    [TestUnity]
    public class PerformanceStageTest : PerformanceStageHiglightingTestBase
    {
        protected override string RelativeTestDataPath => @"CSharp\Daemon\Stages\PerformanceCriticalCodeAnalysis";

        [Test] public void SimpleTest() { DoNamedTest(); }
        [Test] public void SimpleTest2() { DoNamedTest(); }
        [Test] public void CommonTest() { DoNamedTest(); }
        [Test] public void CoroutineTest() { DoNamedTest(); }
        [Test] public void UnityObjectEqTest() { DoNamedTest(); }
        [Test] public void IndirectCostlyTest() { DoNamedTest(); }
      
    }
    
      
    public class PerformanceStageHiglightingTestBase : CSharpHighlightingTestBase
    {
        protected sealed override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile sourceFile,
            IContextBoundSettingsStore settingsStore)
        {
            return highlighting is PerformanceCriticalCodeInvocationReachableHighlighting ||
                   highlighting is PerformanceCriticalCodeInvocationHighlighting;
        }
    }
}