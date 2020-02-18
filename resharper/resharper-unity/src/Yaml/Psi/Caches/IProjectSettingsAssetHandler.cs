using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules;
using JetBrains.ReSharper.Plugins.Yaml.ProjectModel;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches
{
    public interface IProjectSettingsAssetHandler
    {
        bool IsApplicable(IPsiSourceFile sourceFile);
        void Build(IPsiSourceFile sourceFile, ProjectSettingsCacheItem cacheItem);
    }
}