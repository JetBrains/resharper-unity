using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Daemon.Stages.Analysis
{
    [TestUnity]
    public class AttributedMethodSignatureAnalyzerTests : CSharpHighlightingTestBase<IUnityAnalyzerHighlighting>
    {
        protected override string RelativeTestDataPath => @"CSharp\Daemon\Stages\Analysis\AttributedMethodSignatures";

        [Test] public void TestInitializeOnLoadMethodAttributeSignature() { DoNamedTest2(); }
        [Test] public void TestRuntimeInitializeOnLoadMethodAttributeSignature() { DoNamedTest2(); }
        [Test] public void TestOnOpenAssetAttributeSignature() { DoNamedTest2(); }
        [Test, TestUnity(UnityVersion.Unity2018_4)] public void TestSingleOnOpenAssetAttributeSignature() { DoNamedTest2(); }
        [Test] public void TestDidReloadScriptsSignature() { DoNamedTest2(); }
        [Test] public void TestPostProcessSceneAttributeSignature() { DoNamedTest2(); }
        [Test] public void TestPostProcessBuildAttributeSignature() { DoNamedTest2(); }
    }
}