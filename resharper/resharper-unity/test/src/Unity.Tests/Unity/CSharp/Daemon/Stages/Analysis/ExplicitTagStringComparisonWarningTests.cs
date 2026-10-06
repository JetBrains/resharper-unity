using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Daemon.Stages.Analysis
{
    [TestUnity]
    public class ExplicitTagStringComparisonWarningTests : CSharpHighlightingTestBase<ExplicitTagStringComparisonWarning>
    {
        protected override string RelativeTestDataPath => @"CSharp\Daemon\Stages\Analysis";

        [Test, UnityPluginBackendChecklist("C# code analysis", "String literal arguments", "Explicit tag string comparison")] public void TestExplicitTagStringComparisonWarning() { DoNamedTest2(); }
    }
}