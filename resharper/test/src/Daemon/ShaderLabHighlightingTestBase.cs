using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Psi.ShaderLab;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.TestFramework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Daemon
{
    [TestUnity]
    [TestFileExtension(ShaderLabProjectFileType.SHADERLAB_EXTENSION)]
    public abstract class ShaderLabHighlightingTestBase : HighlightingTestBase
    {
        protected override PsiLanguageType CompilerIdsLanguage => ShaderLabLanguage.Instance;
    }
}