using JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Daemon.Stages.Analysis
{
    [TestUnity]
    public class FormerlySerializedAsAttributeProblemAnalyzerTests : CSharpHighlightingTestBase<IUnityHighlighting>
    {
        protected override string RelativeTestDataPath => @"daemon\Stages\Analysis\FormerlySerializedAsAttribute";

        [Test] public void TestNonUnityFields() { DoNamedTest2(); }
        [Test] public void TestRedundantFormerlySerializedAs() { DoNamedTest2(); }
        [Test] public void TestPossibleMisapplicationToMultipleFields() { DoNamedTest2(); }
    }
}