using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Daemon.Stages.Analysis
{
    [TestUnity]
    [TestCustomInspectionSeverity(UnityObjectNullComparisonWarning.HIGHLIGHTING_ID, Severity.DO_NOT_SHOW)]
    public class UnityObjectCompareThisToNullWarningTests : CSharpHighlightingTestBase
    {
        protected override string RelativeTestDataPath => @"CSharp\Daemon\Stages\Analysis";

        [Test] public void TestUnityObjectCompareThisToNullWarning() { DoNamedTest2(); }
    }
}