using JetBrains.Application.Settings;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Rider;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules
{
    [SolutionComponent]
    public class RiderUnityYamlDisableStrategy : UnityYamlDisableStrategy
    {
        private readonly UnityHost myUnityHost;

        public RiderUnityYamlDisableStrategy(Lifetime lifetime, ISolution solution, ISettingsStore settingsStore,
                                             UnityYamlSupport unityYamlSupport, UnityHost unityHost)
            : base(lifetime, solution, settingsStore, unityYamlSupport)
        {
            myUnityHost = unityHost;

            myUnityHost.PerformModelAction(t =>
                t.EnableYamlParsing.Advise(lifetime, _ => unityYamlSupport.IsUnityYamlParsingEnabled.Value = true));
        }

        protected override void NotifyYamlParsingDisabled()
        {
            myUnityHost.PerformModelAction(t => t.NotifyYamlHugeFiles());
        }
    }
}