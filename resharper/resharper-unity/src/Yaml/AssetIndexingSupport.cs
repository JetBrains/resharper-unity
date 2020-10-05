using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Caches;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules;
using JetBrains.ReSharper.Plugins.Yaml.Settings;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml
{
    [SolutionComponent]
    public class AssetIndexingSupport
    {
        public readonly IProperty<bool> IsEnabled;

        public AssetIndexingSupport(Lifetime lifetime, YamlSupport yamlSupport, SolutionCaches solutionCaches,
                                    ISolution solution, SolutionWideWritableContextBoundSettingsStore settingsStore)
        {
            IsEnabled = settingsStore.BoundSettingsStore.GetValueProperty(lifetime,
                (UnitySettings key) => key.IsAssetIndexingEnabled);

            if (!yamlSupport.IsParsingEnabled.Value)
                IsEnabled.Value = false;

            IsEnabled.Change.Advise(lifetime, v =>
            {
                if (v.HasNew && v.New)
                {
                    yamlSupport.IsParsingEnabled.Value = true;
                    if (v.HasOld)
                    {
                        solutionCaches.PersistentProperties[UnityYamlDisableStrategy.SolutionCachesId] =
                            false.ToString();
                    }
                }
            });
        }
    }
}