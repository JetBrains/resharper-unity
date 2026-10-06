using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Daemon.Stages.Analysis
{
    [TestUnity]
    public class FormerlySerializedAsAttributeProblemAnalyzerTests : CSharpHighlightingTestBase<IUnityAnalyzerHighlighting>
    {
        protected override string RelativeTestDataPath => @"CSharp\Daemon\Stages\Analysis\FormerlySerializedAsAttribute";

        [Test, UnityPluginBackendChecklist("C# code analysis", "Serialization", "FormerlySerializedAs attribute problems")] public void TestNonUnityFields() { DoNamedTest2(); } //local stage - swea is not ready
        //1. as is - update gold

        [Test, UnityPluginBackendChecklist("C# code analysis", "Serialization", "FormerlySerializedAs attribute problems")] public void TestRedundantFormerlySerializedAs() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("C# code analysis", "Serialization", "FormerlySerializedAs attribute problems")] public void TestPossibleMisapplicationToMultipleFields() { DoNamedTest2(); }
    }

    [TestUnity]
    public class FormerlySerializedAsAttributeProblemAnalyzerGlobalStageTests : UnitySerializationGlobalStageTestBase<IUnityAnalyzerHighlighting>
    {
        protected override string RelativeTestDataPath => @"CSharp\Daemon\Stages\Analysis\FormerlySerializedAsAttribute";

        [Test, UnityPluginBackendChecklist("C# code analysis", "Serialization", "FormerlySerializedAs attribute problems")] public void TestNonUnityFields() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("C# code analysis", "Serialization", "FormerlySerializedAs attribute problems")] public void TestRedundantFormerlySerializedAs() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("C# code analysis", "Serialization", "FormerlySerializedAs attribute problems")] public void TestPossibleMisapplicationToMultipleFields() { DoNamedTest2(); }
    }
}