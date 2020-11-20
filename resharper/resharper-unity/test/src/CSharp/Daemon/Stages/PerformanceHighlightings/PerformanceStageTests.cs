using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Highlightings;
using JetBrains.ReSharper.Psi;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Daemon.Stages.PerformanceHighlightings
{
    [TestUnity]
    public class PerformanceStageTest : UnityGlobalHighlightingsStageTestBase
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

        protected override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile file, IContextBoundSettingsStore settingsStore)
        {
            return highlighting is UnityPerformanceHighlightingBase;
        }
    }
}
