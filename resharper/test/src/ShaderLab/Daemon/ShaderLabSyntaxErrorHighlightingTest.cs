using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.ShaderLab.Daemon
{
    [TestUnity]
    [TestFileExtension(ShaderLabProjectFileType.SHADERLAB_EXTENSION)]
    public class ShaderLabSyntaxErrorHighlightingTest : HighlightingTestBase
    {
        protected override PsiLanguageType CompilerIdsLanguage => ShaderLabLanguage.Instance;
        protected override string RelativeTestDataPath => @"ShaderLab\Daemon\SyntaxHighlighting";

        [Test] public void TestSyntax01() { DoNamedTest2(); }
        [Test] public void TestSyntax02() { DoNamedTest2(); }
        [Test] public void TestSyntax03() { DoNamedTest2(); }
    }
}