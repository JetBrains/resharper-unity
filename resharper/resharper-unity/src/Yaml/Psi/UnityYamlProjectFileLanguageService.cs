using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.ProjectModel;
using JetBrains.ReSharper.Plugins.Yaml.Resources.Icons;
using JetBrains.ReSharper.Plugins.Yaml.Settings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;
using JetBrains.UI.Icons;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi
{
    [ProjectFileType(typeof(UnityYamlProjectFileType))]
    public class UnityYamlProjectFileLanguageService : ProjectFileLanguageService
    {
        private readonly YamlSupport myYamlSupport;

        public UnityYamlProjectFileLanguageService(YamlSupport yamlSupport)
            : base(UnityYamlProjectFileType.Instance)
        {
            myYamlSupport = yamlSupport;
        }

        public override ILexerFactory GetMixedLexerFactory(ISolution solution, IBuffer buffer, IPsiSourceFile sourceFile = null)
        {
            var languageService = UnityYamlLanguage.Instance.LanguageService();
            return languageService?.GetPrimaryLexerFactory();
        }

        protected override PsiLanguageType PsiLanguageType
        {
            get
            {
                var yamlLanguage = (PsiLanguageType) UnityYamlLanguage.Instance ?? UnknownLanguage.Instance;
                // TODO 
                return myYamlSupport.IsParsingEnabled.Value ? yamlLanguage : UnknownLanguage.Instance;
            }
        }

        public override IconId Icon => YamlFileTypeThemedIcons.FileYaml.Id;
    }
}