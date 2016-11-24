using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;
using JetBrains.UI.Icons;

namespace JetBrains.ReSharper.Plugins.Unity.Psi
{
    [ProjectFileType(typeof(ShaderLabProjectFileType))]
    public class ShaderLabProjectFileLanguageService : ProjectFileLanguageService
    {
        public ShaderLabProjectFileLanguageService()
            : base(ShaderLabProjectFileType.Instance)
        {
        }

        public override ILexerFactory GetMixedLexerFactory(ISolution solution, IBuffer buffer, IPsiSourceFile sourceFile = null)
        {
            var languageService = ShaderLabLanguage.Instance.LanguageService();
            return languageService?.GetPrimaryLexerFactory();
        }

        protected override PsiLanguageType PsiLanguageType => (PsiLanguageType) ShaderLabLanguage.Instance ?? UnknownLanguage.Instance;

        // TODO: Needs a file icon
        public override IconId Icon => null;
    }
}