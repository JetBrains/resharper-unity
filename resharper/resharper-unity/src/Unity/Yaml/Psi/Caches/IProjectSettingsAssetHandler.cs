using JetBrains.Application.Parts;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches
{
    [DerivedComponentsInstantiationRequirement(InstantiationRequirement.DeadlockSafe)]
    public interface IProjectSettingsAssetHandler
    {
        bool IsApplicable(IPsiSourceFile sourceFile);
        void Build(IPsiSourceFile sourceFile, ProjectSettingsCacheItem cacheItem);
    }
}