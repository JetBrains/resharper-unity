using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Daemon.Stages.Analysis
{
    [TestUnity]
    public class DrawGizmoAttributeProblemAnalyzerTests : CSharpHighlightingTestBase<IUnityAnalyzerHighlighting>
    {
        protected override string RelativeTestDataPath => @"CSharp\Daemon\Stages\Analysis\";

        [Test, UnityPluginBackendChecklist("C# code analysis", "Attributes", "DrawGizmo attribute problems")] public void TestDrawGizmoAttributeProblemAnalyzer() { DoNamedTest2(); }
    }
}