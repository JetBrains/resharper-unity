using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Daemon.Stages.Analysis
{
    [TestUnity]
    public class UnityObjectCompareThisToNullWarningTests : CSharpHighlightingTestBase
    {
        protected override string RelativeTestDataPath => @"daemon\Stages\Analysis";

        [Test] public void TestUnityObjectCompareThisToNullWarning() { DoNamedTest2(); }
    }
}