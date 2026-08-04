using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Daemon.Stages.Analysis
{
    [TestUnity]
    public class RedundantSerializeFieldAttributeAnalyzerTests : CSharpHighlightingTestBase<IUnityAnalyzerHighlighting>
    {
        protected override string RelativeTestDataPath => @"CSharp\Daemon\Stages\Analysis";

        [Test] public void TestRedundantSerializeFieldAttribute() { DoNamedTest2(); }

        // Regression guard only. A dictionary resolves to Unknown here rather than NonSerializedField, and this
        // analyzer reports only the latter, so the version-gated behaviour is covered by GutterMarkTests instead.
        [Test, TestUnity(UnityVersion.Unity6000_6)] public void TestRedundantSerializeFieldAttributeDictionary() { DoNamedTest2(); }
    }

    [TestUnity]
    public class RedundantSerializeFieldAttributeGlobalAnalyzerTests : UnitySerializationGlobalStageTestBase<IUnityAnalyzerHighlighting>
    {
        protected override string RelativeTestDataPath => @"CSharp\Daemon\Stages\Analysis";

        [Test] public void TestRedundantSerializeFieldAttribute() { DoNamedTest2(); }

        [Test, TestUnity(UnityVersion.Unity6000_6)] public void TestRedundantSerializeFieldAttributeDictionary() { DoNamedTest2(); }
    }
}