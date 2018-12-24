using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.Platform.RdFramework;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Rider;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Yaml.Psi.Modules
{
    [SolutionComponent]
    public class RiderUnityYamlDisableStrategy : UnityYamlDisableStrategy
    {
        private readonly UnityHost myUnityHost;

        public RiderUnityYamlDisableStrategy(Lifetime lifetime, ISolution solution, ISettingsStore settingsStore, AssetSerializationMode assetSerializationMode,
            UnityYamlEnabled unityYamlEnabled, UnityHost unityHost)
            : base(lifetime, solution, settingsStore, assetSerializationMode, unityYamlEnabled)
        {
            myUnityHost = unityHost;
            
            myUnityHost.PerformModelAction(t => t.EnableYamlParsing.Advise(lifetime, _ =>  YamlParsingEnabled.Value = true));
        }

        protected override void CreateNotification()
        {
            myUnityHost.PerformModelAction(t => t.NotifyYamlHugeFiles.Fire(RdVoid.Instance));
        }
    }
}