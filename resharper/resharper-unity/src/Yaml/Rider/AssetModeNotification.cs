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
        public AssetModeNotification(UnitySolutionTracker solutionTracker, AssetSerializationMode assetSerializationMode, UnityHost unityHost)
        {
            if (!solutionTracker.IsUnityProject.HasTrueValue())
                return;

            if (!assetSerializationMode.IsForceText)
            {
                unityHost.PerformModelAction(t => t.NotifyAssetModeForceText());
            }
        }
    }
}