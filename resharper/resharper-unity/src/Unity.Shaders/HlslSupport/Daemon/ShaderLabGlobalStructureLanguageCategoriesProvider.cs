using JetBrains.ReSharper.Daemon.Stages;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Files;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Daemon
{
    [Language(typeof(ShaderLabLanguage))]
    public class ShaderLabGlobalStructureLanguageCategoriesProvider : IGlobalStructureLanguageCategoriesProvider
    {
        public PsiLanguageCategories GetCategories(IPsiSourceFile sourceFile)
        {
            return PsiLanguageCategories.All;
        }
    }
}