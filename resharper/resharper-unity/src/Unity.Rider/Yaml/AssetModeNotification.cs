using JetBrains.Collections.Viewable;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Rider.Protocol;
using JetBrains.ReSharper.Plugins.Unity.Yaml;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Yaml
{
    [SolutionComponent]
    public class AssetModeNotification
    {
        public AssetModeNotification(UnitySolutionTracker solutionTracker, AssetSerializationMode assetSerializationMode, FrontendBackendHost frontendBackendHost)
        {
            if (!solutionTracker.IsUnityProject.HasTrueValue())
                return;

            if (!assetSerializationMode.IsForceText)
            {
                frontendBackendHost.Do(t => t.NotifyAssetModeForceText());
            }
        }
    }
}