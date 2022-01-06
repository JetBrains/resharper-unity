using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Json.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;
using JetBrains.UI.Icons;

namespace JetBrains.ReSharper.Plugins.Json.Psi
{
    [ProjectFileType(typeof(JsonNewProjectFileType))]
    public class JsonNewProjectFileLanguageService : ProjectFileLanguageService
    {
        public JsonNewProjectFileLanguageService()
            : base(JsonNewProjectFileType.Instance)
        {
        }

        public override ILexerFactory GetMixedLexerFactory(ISolution solution, IBuffer buffer, IPsiSourceFile sourceFile = null)
        {
            var languageService = JsonNewLanguage.Instance.LanguageService();
            return languageService?.GetPrimaryLexerFactory();
        }

        // ReSharper disable once AssignNullToNotNullAttribute
        protected override PsiLanguageType PsiLanguageType =>
            (PsiLanguageType) JsonNewLanguage.Instance ?? UnknownLanguage.Instance;

        // TODO: icon
        public override IconId Icon => null;
    }
}