using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.TestFramework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.ShaderLab.Daemon
{
    [RequireHlslSupport]
    [TestUnity]
    [TestFileExtension(ShaderLabProjectFileType.SHADERLAB_EXTENSION)]
    public abstract class ShaderLabHighlightingTestBase<T> : HighlightingTestBase
    {
        protected override PsiLanguageType CompilerIdsLanguage => ShaderLabLanguage.Instance;

        protected sealed override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile sourceFile,
            IContextBoundSettingsStore settingsStore)
        {
            return highlighting is T;
        }
    }
}