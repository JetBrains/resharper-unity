using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Daemon.Stages.Analysis
{
    [TestUnity]
    public class UnityEventFunctionAnalyzerTests : CSharpHighlightingTestBase<IUnityIndicatorHighlighting>
    {
        protected override string RelativeTestDataPath => @"CSharp\Daemon\Stages\Analysis";

        // ********************************************************************
        // IMPORTANT! Keep in sync with equivalent class in Unity.Rider.Tests
        // ********************************************************************

        [Test, UnityPluginBackendChecklist("C# code analysis", "Event functions", "Event function highlightings")] public void TestUnityEventFunctionAnalyzer() { DoNamedTest2(); }
    }
}