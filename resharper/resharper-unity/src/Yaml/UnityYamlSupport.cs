using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Plugins.Unity.Settings;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml
{
    [SolutionComponent]
    public class UnityYamlSupport
    {
        public readonly IProperty<bool> IsYamlParsingEnabled;
        public UnityYamlSupport(Lifetime lifetime, ISolution solution, ISettingsStore settingsStore)
        {
            var settings = settingsStore.BindToContextLive(lifetime,
                ContextRange.ManuallyRestrictWritesToOneContext(solution.ToDataContext()));
            IsYamlParsingEnabled = settings.GetValueProperty(lifetime, (UnitySettings key) => key.IsYamlParsingEnabled);
        }
    }
}