using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Caches;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml
{
    [SolutionComponent]
    public class AssetIndexingSupport
    {
        public readonly IProperty<bool> IsEnabled;

        public AssetIndexingSupport(Lifetime lifetime,
                                    SolutionCaches solutionCaches,
                                    SolutionWideWritableContextBoundSettingsStore settingsStore)
        {
            // If this property is written to, the changes are saved to solution level settings, specifically .sln.DotSettings.user
            IsEnabled = settingsStore.BoundSettingsStore.GetValueProperty(lifetime,
                (UnitySettings key) => key.IsAssetIndexingEnabled);

            IsEnabled.Change.Advise(lifetime, v =>
            {
                // If we re-enable indexing after it was disabled, disable the heuristics in the solution cache.
                // Otherwise, we'll just run the heuristic again and disbale it again.
                if (v.HasNew && v.New && v.HasOld && !v.Old)
                {
                    solutionCaches.PersistentProperties[UnityYamlDisableStrategy.SolutionCachesId] =
                        false.ToString();
                }
            });
        }
    }
}