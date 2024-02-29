using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Daemon.Stages.Analysis
{
    [TestUnity]
    [TestCustomInspectionSeverity(UnityObjectNullCoalescingWarning.HIGHLIGHTING_ID, Severity.WARNING)]
    public class UnityNullCoalescingWarningTests : CSharpHighlightingTestBase<UnityObjectNullCoalescingWarning>
    {
        protected override string RelativeTestDataPath => @"CSharp\Daemon\Stages\Analysis";

        [Test] public void TestUnityNullCoalescingWarning() { DoNamedTest2(); }
        [Test] public void TestUnityNullCoalescingAssignmentWarning() { DoNamedTest2(); }
    }
}
