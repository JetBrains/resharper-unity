using JetBrains.ReSharper.Daemon.UsageChecking;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Daemon.Stages.Analysis
{
    [TestUnity]
    public class UnityObjectEqualitySuspiciousComparisonWarningTests
        : CSharpHighlightingTestBase<SuspiciousComparisonWarning>
    {
        protected override string RelativeTestDataPath => @"CSharp\Daemon\Stages\Analysis";

        [Test] public void TestUnityObjectEqualitySuspiciousComparisonWarning() { DoNamedTest2(); }
    }
}