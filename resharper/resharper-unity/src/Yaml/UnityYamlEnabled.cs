using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Plugins.Unity.Settings;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml
{
    [SolutionComponent]
    public class UnityYamlEnabled
    {
        public readonly IProperty<bool> YamlParsingEnabled;
        public UnityYamlEnabled(Lifetime lifetime, ISolution solution, ISettingsStore settingsStore)
        {
            var settings = settingsStore.BindToContextLive(lifetime,
                ContextRange.ManuallyRestrictWritesToOneContext(solution.ToDataContext()));
            YamlParsingEnabled = settings.GetValueProperty(lifetime, (UnitySettings key) => key.EnableYamlParsing);
        }
    }
}