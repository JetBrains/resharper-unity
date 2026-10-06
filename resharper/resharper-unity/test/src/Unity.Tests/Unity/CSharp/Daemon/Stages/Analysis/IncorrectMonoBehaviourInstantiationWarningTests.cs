using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Daemon.Stages.Analysis
{
    [TestUnity]
    public class IncorrectMonoBehaviourInstantiationWarningTests : CSharpHighlightingTestBase<IUnityAnalyzerHighlighting>
    {
        protected override string RelativeTestDataPath => @"CSharp\Daemon\Stages\Analysis";

        [Test, UnityPluginBackendChecklist("C# code analysis", "Incorrect instantiation", "MonoBehaviour instantiation")] public void TestInstantiateMonoBehaviourWarning() { DoNamedTest2(); }
    }
}