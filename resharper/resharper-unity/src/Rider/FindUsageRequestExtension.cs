using JetBrains.Platform.Unity.EditorPluginModel;
using JetBrains.Rider.Model;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    public static class FindUsageRequestExtension
    {
        public static RdFindUsageRequestBase ConvertToUnityModel(this FindUsageRequestBase request)
        {
            if (request is FindUsageRequestScene requestScene)
            {
                return new RdFindUsageRequestScene(requestScene.LocalId, requestScene.ExpandInTreeView, requestScene.FilePath, requestScene.PathElements);
            }

            if (request is FindUsageRequestPrefab requestPrefab)
            {
                return new RdFindUsageRequestPrefab(requestPrefab.ExpandInTreeView, requestPrefab.FilePath, requestPrefab.PathElements);
            }

            return null;
        }
    }
}