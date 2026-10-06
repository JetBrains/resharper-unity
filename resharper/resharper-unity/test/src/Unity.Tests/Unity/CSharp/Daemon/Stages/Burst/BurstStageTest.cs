using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Highlightings;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Daemon.Stages.Burst
{
    [TestUnity]
    public class BurstStageTest : UnityGlobalHighlightingsStageTestBase<IBurstHighlighting>
    {
        protected override string RelativeTestDataPath => @"CSharp\Daemon\Stages\BurstCodeAnalysis\";

        [Test, UnityPluginBackendChecklist("DOTS and Burst", "Burst code analysis")] public void SmartMarkingTests() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("DOTS and Burst", "Burst code analysis")] public void PrimitivesTests() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("DOTS and Burst", "Burst code analysis")] public void ReferenceExpressionTests() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("DOTS and Burst", "Burst code analysis")] public void MethodInvocationTests() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("DOTS and Burst", "Burst code analysis")] public void FunctionParametersReturnTests() { DoNamedTest(); }
        [Ignore("Try/finally, using and foreach are allowed fom burst 1.4")][Test, UnityPluginBackendChecklist("DOTS and Burst", "Burst code analysis")] public void ExceptionsTests() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("DOTS and Burst", "Burst code analysis")] public void EqualsTests() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("DOTS and Burst", "Burst code analysis")] public void DirectivesTests() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("DOTS and Burst", "Burst code analysis")] public void BurstDiscardTests() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("DOTS and Burst", "Burst code analysis")] public void DebugStringTests() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("DOTS and Burst", "Burst code analysis")] public void TypeofTests() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("DOTS and Burst", "Burst code analysis")] public void SharedStaticCreateTests() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("DOTS and Burst", "Burst code analysis")] public void NullableTests() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("DOTS and Burst", "Burst code analysis")] public void ConditionalAttributesTests() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("DOTS and Burst", "Burst code analysis")] public void CommentRootsTests() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("DOTS and Burst", "Burst code analysis regressions")] public void BugRider53010() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("DOTS and Burst", "Burst code analysis regressions")] public void BugRider68193() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("DOTS and Burst", "Burst code analysis regressions")] public void BugRider68095() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("DOTS and Burst", "Burst code analysis regressions")] public void BugRider92491() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("DOTS and Burst", "Burst code analysis regressions")] public void BugRider92491_2() { DoNamedTest(); }
        // Bug - youtrack
        // Issue - github.com/jetbrains/resharper-unity
        [Test, UnityPluginBackendChecklist("DOTS and Burst", "Burst code analysis regressions")] public void IssueRider2181() { DoNamedTest(); }
        
        [Test, UnityPluginBackendChecklist("DOTS and Burst", "Burst code analysis regressions")] public void BugRider106221() { DoNamedTest(); }
        
        [Test, UnityPluginBackendChecklist("DOTS and Burst", "Burst code analysis regressions")] public void BugRider113317WithoutBurst() { DoNamedTest(); }
    }
}