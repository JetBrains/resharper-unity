using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis;
using JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Daemon.Stages.Analysis;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis
{
    [TestUnity]
    public class PerformanceStageTest : CSharpHighlightingTestWithProductDependentGoldBase<PerformanceHighlightingBase>
    {
        protected override string RelativeTestDataRoot => @"CSharp\Daemon\Stages\PerformanceCriticalCodeAnalysis";

        [Test] public void SimpleTest() { DoNamedTest(); }
        [Test] public void SimpleTest2() { DoNamedTest(); }
        [Test] public void CommonTest() { DoNamedTest(); }
        [Test] public void CoroutineTest() { DoNamedTest(); }
        [Test] public void UnityObjectEqTest() { DoNamedTest(); }
        [Test] public void IndirectCostlyTest() { DoNamedTest(); }
        [Test] public void InefficientCameraMainUsageWarningTest() {DoNamedTest();}
        [Test] public void InvokeAndSendMessageTest() {DoNamedTest();}
        [Test] public void DisabledWarningTest() {DoNamedTest();}
    }
}