using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Caches;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules;
using JetBrains.ReSharper.Plugins.Yaml.Settings;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml
{
    [SolutionComponent]
    public class UnityYamlSupport
    {
        public readonly IProperty<bool> IsUnityYamlParsingEnabled;

        public UnityYamlSupport(Lifetime lifetime, YamlSupport yamlSupport, SolutionCaches solutionCaches, ISolution solution, ISettingsStore settingsStore)
        {
            var settings = settingsStore.BindToContextLive(lifetime,
                ContextRange.ManuallyRestrictWritesToOneContext(solution.ToDataContext()));
            IsUnityYamlParsingEnabled = settings.GetValueProperty(lifetime, (UnitySettings key) => key.IsYamlParsingEnabled);

            if (!yamlSupport.IsParsingEnabled.Value)
                IsUnityYamlParsingEnabled.Value = false;

            IsUnityYamlParsingEnabled.Change.Advise(lifetime, v =>
            {
                if (v.HasNew && v.New)
                {
                    yamlSupport.IsParsingEnabled.Value = true;
                    if (v.HasOld)
                    {
                        solutionCaches.PersistentProperties[UnityYamlDisableStrategy.SolutionCachesId] = false.ToString();
                    }
                }
            });
        }
    }
}