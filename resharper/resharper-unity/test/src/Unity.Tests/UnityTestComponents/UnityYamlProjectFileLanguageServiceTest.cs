using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Tests.UnityTestComponents
{
    // TODO: Consider removing this. See ParserTests
    [ProjectFileType(typeof(UnityYamlProjectFileType))]
    public class UnityYamlProjectFileLanguageServiceTest : UnityYamlProjectFileLanguageService
    {
        public override PsiLanguageType GetPsiLanguageType(ProjectFileType languageType)
        {
            return languageType.Is<UnityYamlProjectFileType>()
                ? YamlLanguage.Instance.NotNull("YamlLanguage.Instance != null")
                : base.GetPsiLanguageType(languageType);
        }
    }
}