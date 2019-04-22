using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches
{
    [SolutionComponent]
    public class InputManagerAssetHandler : IProjectSettingsAssetHandler
    {
        public bool IsApplicable(IPsiSourceFile sourceFile)
        {
            return sourceFile.Name.Equals("InputManager.asset");
        }

        public ProjectSettingsCacheItem Build(IPsiSourceFile sourceFile)
        {
            return null;
        }
    }
}