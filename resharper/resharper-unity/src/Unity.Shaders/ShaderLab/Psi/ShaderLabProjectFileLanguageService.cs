#nullable enable
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Integration.Cpp;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Settings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Resources;
using JetBrains.Text;
using JetBrains.UI.Icons;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi
{
    [ProjectFileType(typeof(ShaderLabProjectFileType))]
    public class ShaderLabProjectFileLanguageService : ProjectFileLanguageService
    {
        private readonly ShaderLabSupport myShaderLabSupport;

        public ShaderLabProjectFileLanguageService(ShaderLabSupport shaderLabSupport)
            : base(ShaderLabProjectFileType.Instance)
        {
            myShaderLabSupport = shaderLabSupport;
        }

        public override ILexerFactory? GetMixedLexerFactory(ISolution solution, IBuffer buffer, IPsiSourceFile? sourceFile = null)
        {
            var languageService = ShaderLabLanguage.Instance.LanguageService();
            return languageService?.GetPrimaryLexerFactory();
        }

        protected override PsiLanguageType PsiLanguageType => myShaderLabSupport.IsParsingEnabled.Value && ShaderLabLanguage.Instance is { } shaderLabLanguage ? shaderLabLanguage : UnknownLanguage.Instance!;

        /// Alexander.Bondarev:
        /// todo: it only defines default properties for ShaderLab files, actual properties provided in <see cref="UnityShaderModuleHandlerAndDecorator.GetFileProperties"/>.
        /// todo: it isn't clear without debugging if we can always provide properties in this method (with caching) or it is intended in <see cref="UnityShaderModuleHandlerAndDecorator.GetFileProperties"/> to always recalculate properties
        /// todo: for hotfix 23.2 I decided not risk to change behavior and only provide a fallback for shader files outside of Shaders module (i.e. for Player project files).  
        public override IPsiSourceFileProperties? GetPsiProperties(IProjectFile projectFile, IPsiSourceFile sourceFile, IsCompileService isCompileService) => ShaderFilesProperties.NoCacheFilesProperties;

        public override IconId? Icon => PsiSymbolsThemedIcons.FileShader.Id;
    }
}