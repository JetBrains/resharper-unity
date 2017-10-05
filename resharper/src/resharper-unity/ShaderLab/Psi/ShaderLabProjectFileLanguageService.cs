using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;
using JetBrains.UI.Icons;
using JetBrains.UI.ThemedIcons;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi
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

        public override ILexerFactory GetMixedLexerFactory(ISolution solution, IBuffer buffer, IPsiSourceFile sourceFile = null)
        {
            var languageService = ShaderLabLanguage.Instance.LanguageService();
            return languageService?.GetPrimaryLexerFactory();
        }

        protected override PsiLanguageType PsiLanguageType
        {
            get
            {
                var shaderLabLanguage = (PsiLanguageType) ShaderLabLanguage.Instance ?? UnknownLanguage.Instance;
                return myShaderLabSupport.IsParsingEnabled.Value
                    ? shaderLabLanguage
                    : UnknownLanguage.Instance;
            }
        }

        public override IconId Icon => DivebomThemedIconsThemedIcons.Shader.Id;
    }
}