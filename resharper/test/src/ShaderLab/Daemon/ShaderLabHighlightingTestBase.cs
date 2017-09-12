using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.TestFramework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.ShaderLab.Daemon
{
    [TestUnity]
    [TestFileExtension(ShaderLabProjectFileType.SHADER_EXTENSION)]
    public abstract class ShaderLabHighlightingTestBase : HighlightingTestBase
    {
        protected override PsiLanguageType CompilerIdsLanguage => ShaderLabLanguage.Instance;
    }
}