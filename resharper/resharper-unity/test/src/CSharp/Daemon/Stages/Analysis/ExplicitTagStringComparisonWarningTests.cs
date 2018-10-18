using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Daemon.Stages.Analysis
{
    [TestUnity]
    public class ExplicitTagStringComparisonWarningTests : CSharpHighlightingTestBase<ExplicitTagStringComparisonWarning>
    {
        protected override string RelativeTestDataPath => @"CSharp\Daemon\Stages\Analysis";

        [Test] public void TestExplicitTagStringComparisonWarning() { DoNamedTest2(); }
    }
}