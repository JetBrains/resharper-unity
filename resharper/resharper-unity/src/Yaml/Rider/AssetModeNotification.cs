using JetBrains.Application.Settings;
using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Rider;
using JetBrains.ReSharper.Plugins.Unity.Settings;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml
{
    [SolutionComponent]
    public class AssetModeNotification
    {
        public AssetModeNotification(UnitySolutionTracker solutionTracker, AssetSerializationMode assetSerializationMode,
            ISettingsStore settingsStore, UnityHost unityHost)
        {
            if (!solutionTracker.IsUnityProject.HasTrueValue())
                return;

            var settings = settingsStore.BindToContextTransient(ContextRange.ApplicationWide);
            var enabled = settings.GetValue((UnitySettings key) => key.IsAssetModeNotificationEnabled);

            if (enabled)
            {
                if (!assetSerializationMode.IsForceText)
                {
                    settings.SetValue((UnitySettings key) => key.IsAssetModeNotificationEnabled, false);
                    unityHost.PerformModelAction(t => t.NotifyAssetModeForceText());
                }
            }
        }
    }
}