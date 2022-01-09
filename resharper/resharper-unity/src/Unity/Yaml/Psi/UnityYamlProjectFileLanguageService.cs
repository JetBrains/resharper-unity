using System;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.Plugins.Unity.Yaml.ProjectModel;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
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
        public UnityYamlProjectFileLanguageService()
            : base(UnityYamlProjectFileType.Instance)
        {
        }

        public override ILexerFactory GetMixedLexerFactory(ISolution solution, IBuffer buffer, IPsiSourceFile sourceFile = null)
        {
            var languageService = UnityYamlLanguage.Instance.LanguageService();
            return languageService?.GetPrimaryLexerFactory();
        }

        public override PsiLanguageType GetPsiLanguageType(IPsiSourceFile sourceFile)
        {
            if (UnityFileExtensions.IsMetaOrProjectSettings(sourceFile.GetSolution(), sourceFile.GetLocation()))
                return base.GetPsiLanguageType(sourceFile);

            return UnityYamlLanguage.Instance ?? throw new InvalidOperationException("Unexpected state");
        }

        // ReSharper disable once AssignNullToNotNullAttribute
        protected override PsiLanguageType PsiLanguageType =>
            (PsiLanguageType) YamlLanguage.Instance ?? UnknownLanguage.Instance;

        public override IconId Icon => YamlFileTypeThemedIcons.FileYaml.Id;
    }
}