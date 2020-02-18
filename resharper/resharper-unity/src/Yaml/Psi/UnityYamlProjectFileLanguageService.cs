using System;
using System.Linq;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.ProjectModel;
using JetBrains.ReSharper.Plugins.Yaml.Resources.Icons;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;
using JetBrains.UI.Icons;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi
{
    [ProjectFileType(typeof(UnityYamlProjectFileType))]
    public class UnityYamlProjectFileLanguageService : ProjectFileLanguageService
    {
        public UnityYamlProjectFileLanguageService() : base(UnityYamlProjectFileType.Instance)
        {
        }

        public override ILexerFactory GetMixedLexerFactory(ISolution solution, IBuffer buffer, IPsiSourceFile sourceFile = null)
        {
            var languageService = UnityYamlLanguage.Instance.LanguageService();
            return languageService?.GetPrimaryLexerFactory();
        }

        public override PsiLanguageType GetPsiLanguageType(IPsiSourceFile sourceFile)
        {
            var location = sourceFile.GetLocation();
            var components = location.MakeRelativeTo(sourceFile.GetSolution().SolutionDirectory).Components.ToArray();
            if (location.ExtensionNoDot.Equals("meta") || components.Length == 2 && components[0].Equals("ProjectSettings"))
                return base.GetPsiLanguageType(sourceFile);
            
            return UnityYamlDummyLanguage.Instance ?? throw new InvalidOperationException("Unexpected state");
        }

        protected override PsiLanguageType PsiLanguageType
        {
            get
            {
                var yamlLanguage = (PsiLanguageType) UnityYamlLanguage.Instance ?? UnknownLanguage.Instance;
                return yamlLanguage ?? throw new InvalidOperationException("Unexpected state");
            }
        }

        public override IconId Icon => YamlFileTypeThemedIcons.FileYaml.Id;
    }
}