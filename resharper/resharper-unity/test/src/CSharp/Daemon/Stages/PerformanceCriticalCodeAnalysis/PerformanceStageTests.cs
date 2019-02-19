using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis;
using JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Daemon.Stages.Analysis;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis
{
    [TestUnity]
    public class PerformanceStageTest : PerformanceCriticalCodeStageTestBase
    {
        private const string ProductGoldSuffix =
#if RIDER
                "rider"
#else
                "resharper"
#endif
            ;

        protected override string RelativeTestDataPath => $@"CSharp\Daemon\Stages\PerformanceCriticalCodeAnalysis\{ProductGoldSuffix}";

        [Test] public void SimpleTest() { DoNamedTest(); }
        [Test] public void SimpleTest2() { DoNamedTest(); }
        [Test] public void CommonTest() { DoNamedTest(); }
        [Test]public void CoroutineTest() { DoNamedTest(); }
        [Test] public void UnityObjectEqTest() { DoNamedTest(); }
        [Test] public void IndirectCostlyTest() { DoNamedTest(); }
        [Test] public void InefficientCameraMainUsageWarningTest() {DoNamedTest();}
        [Test]public void InvokeAndSendMessageTest() {DoNamedTest();}
        [Test] public void DisabledWarningTest() {DoNamedTest();}
    }
}