using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches
{
    public interface IProjectSettingsAssetHandler
    {
        bool IsApplicable(IPsiSourceFile sourceFile);
        void Build(IPsiSourceFile sourceFile, ProjectSettingsCacheItem cacheItem);
    }
}