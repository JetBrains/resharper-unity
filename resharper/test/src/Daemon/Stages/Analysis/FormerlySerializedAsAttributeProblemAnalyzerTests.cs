using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Psi;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Daemon.Stages.Analysis
{
    [TestUnity]
    public class FormerlySerializedAsAttributeProblemAnalyzerTests : CSharpHighlightingTestBase
    {
        protected override string RelativeTestDataPath => @"daemon\Stages\Analysis\FormerlySerializedAsAttribute";

        protected override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile sourceFile)
        {
            return highlighting is IUnityHighlighting;
        }

        [Test] public void TestNonUnityFields() { DoNamedTest2(); }
        [Test] public void TestRedundantFormerlySerializedAs() { DoNamedTest2(); }
        [Test] public void TestPossibleMisapplicationToMultipleFields() { DoNamedTest2(); }
    }
}