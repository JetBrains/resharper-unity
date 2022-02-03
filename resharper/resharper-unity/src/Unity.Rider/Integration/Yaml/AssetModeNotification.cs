using JetBrains.Collections.Viewable;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Protocol;
using JetBrains.ReSharper.Plugins.Unity.Yaml;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Yaml
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