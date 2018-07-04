using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Daemon.Stages.Analysis
{
    [TestUnity]
    public class UnityObjectCompareThisToNullWarningTests : CSharpHighlightingTestBase
    {
        protected override string RelativeTestDataPath => @"CSharp\Daemon\Stages\Analysis";

        [Test] public void TestUnityObjectCompareThisToNullWarning() { DoNamedTest2(); }
    }
}