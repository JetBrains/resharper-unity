using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Daemon.Stages.Analysis
{
    [TestUnity]
    public class RedundantHideInInspectorAttributeAnalyzerTests : CSharpHighlightingTestWithProductDependentGoldBase<IUnityAnalyzerHighlighting>
    {
        protected override string RelativeTestDataRoot => @"CSharp\Daemon\Stages\Analysis";

        [Test] public void TestRedundantHideInInspectorAttribute() { DoNamedTest2(); }
    }
}