using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Highlightings;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Daemon.Stages.PerformanceHighlightings
{
    [TestUnity]
    public class PerformanceStageTest : UnityGlobalHighlightingsStageTestBase<IUnityPerformanceHighlighting>
    {
        protected override string RelativeTestDataPath => @"CSharp\Daemon\Stages\PerformanceCriticalCodeAnalysis\";

        // ********************************************************************
        // IMPORTANT! Keep in sync with equivalent class in Unity.Rider.Tests
        // ********************************************************************

        [Test, UnityPluginBackendChecklist("Performance critical code analysis", "Performance critical context detection")] public void SimpleTest() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Performance critical code analysis", "Performance critical context detection in nested scopes")] public void SimpleTest2() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Performance critical code analysis", "Common performance warnings")] public void CommonTest() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Performance critical code analysis", "Coroutines")] public void CoroutineTest() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Performance critical code analysis", "Unity object equality comparison")] public void UnityObjectEqTest() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Performance critical code analysis", "Indirect costly method calls")] public void IndirectCostlyTest() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Performance critical code analysis", "Inefficient Camera.main usage")] public void InefficientCameraMainUsageWarningTest() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Performance critical code analysis", "Invoke and SendMessage string literals")] public void InvokeAndSendMessageTest() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Performance critical code analysis", "Disabled performance warnings")] public void DisabledWarningTest() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Performance critical code analysis", "Lambdas")] public void LambdasTest() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Performance critical code analysis", "Local functions")] public void LocalFunctionsTest() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Performance critical code analysis", "Comment-based performance roots")] public void CommentRootsTest() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Performance critical code analysis", "Editor classes are excluded from analysis")] public void EditorClassesTest() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Performance critical code analysis", "Comment-based performance roots with nested comments")] public void CommentRootsTest2() { DoNamedTest(); }
        // this test gold does not contain ".gen" part!
        // gold - "SimpleGenTest.cs.gold"
        // but test file - "SimpleGenTest.gen.cs"
        [Test, UnityPluginBackendChecklist("Performance critical code analysis", "Generated files analysis")] public void SimpleGenTest() { DoOneTest(nameof(SimpleGenTest) + ".gen"); }
    }
}
