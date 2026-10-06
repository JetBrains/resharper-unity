using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Daemon.Stages.Analysis
{
    [TestUnity]
    public class IncorrectScriptableObjectInstantiationWarningTests : CSharpHighlightingTestBase<IUnityAnalyzerHighlighting>
    {
        protected override string RelativeTestDataPath => @"CSharp\Daemon\Stages\Analysis";

        [Test, UnityPluginBackendChecklist("C# code analysis", "Incorrect instantiation", "ScriptableObject instantiation")] public void TestInstantiateScriptableObjectWarning() { DoNamedTest2(); }
    }
}