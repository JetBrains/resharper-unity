using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Daemon.Stages.Analysis
{
    [TestUnity]
    public class FormerlySerializedAsAttributeProblemAnalyzerTests : CSharpHighlightingTestBase<IUnityHighlighting>
    {
        protected override string RelativeTestDataPath => @"CSharp\Daemon\Stages\Analysis\FormerlySerializedAsAttribute";

        [Test] public void TestNonUnityFields() { DoNamedTest2(); }
        [Test] public void TestRedundantFormerlySerializedAs() { DoNamedTest2(); }
        [Test] public void TestPossibleMisapplicationToMultipleFields() { DoNamedTest2(); }
    }
}