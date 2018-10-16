using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Daemon.Stages.Analysis
{
    [TestUnity]
    public class AttributedMethodSignatureAnalyzerTests : CSharpHighlightingTestBase<IUnityHighlighting>
    {
        protected override string RelativeTestDataPath => @"CSharp\Daemon\Stages\Analysis\AttributedMethodSignatures";

        [Test] public void TestInitializeOnLoadMethodAttributeSignature() { DoNamedTest2(); }
        [Test] public void TestRuntimeInitializeOnLoadMethodAttributeSignature() { DoNamedTest2(); }
        [Test] public void TestOnOpenAssetAttributeSignature() { DoNamedTest2(); }
        [Test] public void TestDidReloadScriptsSignature() { DoNamedTest2(); }
        [Test] public void TestPostProcessSceneAttributeSignature() { DoNamedTest2(); }
        [Test] public void TestPostProcessBuildAttributeSignature() { DoNamedTest2(); }
    }
}