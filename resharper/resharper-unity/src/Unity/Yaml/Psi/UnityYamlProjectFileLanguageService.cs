using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.ProjectModel;
using JetBrains.ReSharper.Plugins.Yaml.Resources.Icons;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;
using JetBrains.UI.Icons;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi
{
    [ProjectFileType(typeof(UnityYamlProjectFileType))]
    public class UnityYamlProjectFileLanguageService : ProjectFileLanguageService
    {
        public UnityYamlProjectFileLanguageService()
            : base(UnityYamlProjectFileType.Instance)
        {
        }

        public override ILexerFactory? GetMixedLexerFactory(ISolution solution, IBuffer buffer, IPsiSourceFile? sourceFile = null)
        {
            var languageService = UnityYamlLanguage.Instance.LanguageService();
            return languageService?.GetPrimaryLexerFactory();
        }

        protected override PsiLanguageType PsiLanguageType =>
            (PsiLanguageType?) UnityYamlLanguage.Instance ?? UnknownLanguage.Instance!;

        public override IconId? Icon => YamlFileTypeThemedIcons.FileYaml.Id;
    }
}