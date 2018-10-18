using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Daemon.Stages.Analysis
{
    [TestUnity]
    public class RedundantSerializeFieldAttributeAnalyzerTests : CSharpHighlightingTestBase<IUnityHighlighting>
    {
        protected override string RelativeTestDataPath => @"CSharp\Daemon\Stages\Analysis";

        [Test] public void TestRedundantSerializeFieldAttribute() { DoNamedTest2(); }
    }
}