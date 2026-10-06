using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.CommonCodeAnalysis.Highlightings;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Daemon.Stages.Analysis
{
    [TestUnity]
    public class CommonProblemAnalyzersTests : UnityGlobalHighlightingsStageTestBase<ICommonCodeHighlighting>
    {
        protected override string RelativeTestDataPath => @"CSharp\Daemon\Stages\CommonCodeAnalysis\";

        [Test, UnityPluginBackendChecklist("C# code analysis", "Common problem analyzers")] public void SharedStaticTests() { DoNamedTest(); }
    }
}