using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Highlightings;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Daemon.Stages.PerformanceHighlightings
{
    [TestUnity]
    public class PerformanceStageTest : UnityGlobalHighlightingsStageTestBase<UnityPerformanceHighlightingBase>
    {
        protected override string RelativeTestDataRoot => @"CSharp\Daemon\Stages\PerformanceCriticalCodeAnalysis\";

        [Test] public void SimpleTest() { DoNamedTest(); }
        [Test] public void SimpleTest2() { DoNamedTest(); }
        [Test] public void CommonTest() { DoNamedTest(); }
        [Test]public void CoroutineTest() { DoNamedTest(); }
        [Test] public void UnityObjectEqTest() { DoNamedTest(); }
        [Test] public void IndirectCostlyTest() { DoNamedTest(); }
        [Test] public void InefficientCameraMainUsageWarningTest() {DoNamedTest();}
        [Test]public void InvokeAndSendMessageTest() {DoNamedTest();}
        [Test] public void DisabledWarningTest() {DoNamedTest();}
        [Test] public void LambdasTest() {DoNamedTest();}
        [Test] public void LocalFunctionsTest() {DoNamedTest();}
        [Test] public void CommentRootsTest() { DoNamedTest(); }
        [Test] public void EditorClassesTest() {DoNamedTest();}
        [Test] public void CommentRootsTest2() { DoNamedTest(); }
        // this test gold does not contain ".gen" part!
        // gold - "SimpleGenTest.cs.gold"
        // but test file - "SimpleGenTest.gen.cs"
        [Test] public void SimpleGenTest() { DoOneTest(nameof(SimpleGenTest) + ".gen"); }
    }
}
