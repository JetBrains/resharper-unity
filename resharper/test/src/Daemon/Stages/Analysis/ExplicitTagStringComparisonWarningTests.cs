using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Daemon.Stages.Analysis
{
    [TestUnity]
    public class ExplicitTagStringComparisonWarningTests : CSharpHighlightingTestBase<ExplicitTagStringComparisonWarning>
    {
        protected override string RelativeTestDataPath => @"daemon\Stages\Analysis";

        [Test] public void TestExplicitTagStringComparisonWarning() { DoNamedTest2(); }
    }
}