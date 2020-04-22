using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Settings;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Yaml
{
    
    [ProjectFileType(typeof(UnityYamlProjectFileType))]
    public class UnityYamlProjectFileLanguageServiceTest : UnityYamlProjectFileLanguageService
    {
        public override PsiLanguageType GetPsiLanguageType(IPsiSourceFile sourceFile)
        {
            IProjectFile projectFile = sourceFile.ToProjectFile();
            return projectFile == null ? GetPsiLanguageType(sourceFile.LanguageType) : GetPsiLanguageType(projectFile);
        }

        public UnityYamlProjectFileLanguageServiceTest(YamlSupport yamlSupport)
            : base(yamlSupport)
        {
        }
    }
}