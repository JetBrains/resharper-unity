using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Daemon.Stages.Analysis
{
    [TestUnity]
    public class RedundantHideInInspectorAttributeAnalyzerTests : CSharpHighlightingTestBase<IUnityAnalyzerHighlighting>
    {
        protected override string RelativeTestDataPath => @"CSharp\Daemon\Stages\Analysis";

        [Test] public void TestRedundantHideInInspectorAttribute() { DoNamedTest2(); }
    }
    
    [TestUnity]
    public class RedundantHideInInspectorAttributeGlobalAnalyzerTests : UnitySerializationGlobalStageTestBase<IUnityAnalyzerHighlighting>
    {
        protected override string RelativeTestDataPath => @"CSharp\Daemon\Stages\Analysis";

        [Test] public void TestRedundantHideInInspectorAttribute() { DoNamedTest2(); }
    }
}