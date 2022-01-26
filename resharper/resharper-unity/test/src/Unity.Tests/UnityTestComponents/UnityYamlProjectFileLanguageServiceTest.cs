using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Tests.UnityTestComponents
{
    [ProjectFileType(typeof(UnityYamlProjectFileType))]
    public class UnityYamlProjectFileLanguageServiceTest : UnityYamlProjectFileLanguageService
    {
        public override PsiLanguageType GetPsiLanguageType(IPsiSourceFile sourceFile)
        {
            IProjectFile projectFile = sourceFile.ToProjectFile();
            return projectFile == null ? GetPsiLanguageType(sourceFile.LanguageType) : GetPsiLanguageType(projectFile);
        }
    }
}