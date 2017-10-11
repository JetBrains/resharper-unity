using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Psi;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Daemon.Stages.Analysis
{
    [TestUnity]
    public class AttributedMethodSignatureAnalyzerTests : CSharpHighlightingTestBase
    {
        protected override string RelativeTestDataPath => @"daemon\Stages\Analysis\AttributedMethodSignatures";

        protected override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile sourceFile)
        {
            return highlighting is IUnityHighlighting;
        }

        [Test] public void TestInitializeOnLoadMethodAttributeSignature() { DoNamedTest2(); }
        [Test] public void TestRuntimeInitializeOnLoadMethodAttributeSignature() { DoNamedTest2(); }
        [Test] public void TestOnOpenAssetAttributeSignature() { DoNamedTest2(); }
        [Test] public void TestDidReloadScriptsSignature() { DoNamedTest2(); }
    }
}