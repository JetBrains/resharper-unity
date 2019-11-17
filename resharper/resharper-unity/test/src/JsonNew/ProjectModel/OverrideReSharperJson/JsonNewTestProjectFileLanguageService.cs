using JetBrains.Application.Components;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.JavaScript.LanguageImpl.JSon;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.JsonNew.ProjectModel.OverrideReSharperJson
{
    [ProjectFileType(typeof (JsonProjectFileType))]
    public class JsonNewTestProjectFileLanguageService : JsonProjectFileLanguageService, IHideImplementation<JsonNewProjectFileLanguageService>
    {
        public JsonNewTestProjectFileLanguageService(JsonProjectFileType jsonProjectFileType)
            : base(jsonProjectFileType)
        {
        }

        public override PsiLanguageType GetPsiLanguageType(IProjectFile projectFile)
        {
            return JsonNewLanguage.Instance;
        }

        public override PsiLanguageType GetPsiLanguageType(IPsiSourceFile sourceFile)
        {
            return JsonNewLanguage.Instance;
        }

        public override PsiLanguageType GetPsiLanguageType(ProjectFileType languageType)
        {
            return JsonNewLanguage.Instance;
        }

        public override ILexerFactory GetMixedLexerFactory(ISolution solution, IBuffer buffer, IPsiSourceFile sourceFile = null)
        {

            return JsonNewLanguage.Instance.LanguageService().NotNull("JsonNewLanguage.Instance.LanguageService() != null").GetPrimaryLexerFactory();
        }
    }
}