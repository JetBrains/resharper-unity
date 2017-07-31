using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;
using JetBrains.UI.Icons;

namespace JetBrains.ReSharper.Plugins.Unity.Psi.Cg
{
    [ProjectFileType(typeof(CgProjectFileType))]
    public class CgProjectFileLanguageService : ProjectFileLanguageService
    {
        public CgProjectFileLanguageService()
            : base(CgProjectFileType.Instance)
        {
        }

        public override ILexerFactory GetMixedLexerFactory(ISolution solution, IBuffer buffer, IPsiSourceFile sourceFile = null)
        {
            var languageService = CgLanguage.Instance.LanguageService();
            return languageService?.GetPrimaryLexerFactory();
        }

        protected override PsiLanguageType PsiLanguageType => 
            (PsiLanguageType) CgLanguage.Instance ?? UnknownLanguage.Instance;

        // TODO: icon
        public override IconId Icon => null;
    }
}