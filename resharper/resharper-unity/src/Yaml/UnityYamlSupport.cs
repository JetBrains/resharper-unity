using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Plugins.Yaml.Settings;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml
{
    [SolutionComponent]
    public class UnityYamlSupport
    {
        public readonly IProperty<bool> IsYamlParsingEnabled;
        public UnityYamlSupport(Lifetime lifetime, YamlSupport support, ISolution solution, ISettingsStore settingsStore)
        {
            var settings = settingsStore.BindToContextLive(lifetime,
                ContextRange.ManuallyRestrictWritesToOneContext(solution.ToDataContext()));
            IsYamlParsingEnabled = settings.GetValueProperty(lifetime, (UnitySettings key) => key.IsYamlParsingEnabled);
            
            
            if (!support.IsParsingEnabled.Value)
                IsYamlParsingEnabled.Value = false;

            IsYamlParsingEnabled.Change.Advise(lifetime, v =>
            {
                if (v.HasNew && v.New)
                {
                    support.IsParsingEnabled.Value = true;
                }
            });
        }
    }
}