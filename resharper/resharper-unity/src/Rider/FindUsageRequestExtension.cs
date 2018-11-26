using JetBrains.Platform.Unity.EditorPluginModel;
using JetBrains.Rider.Model;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    public static class FindUsageRequestExtension
    {
        public static RdFindUsageRequest ConvertToUnityModel(this FindUsageRequest request)
        {
            return new RdFindUsageRequest(request.IsPrefab, request.ExpandInTreeView, request.FilePath, request.FileName, request.PathElements, request.RootIndices);
        }
    }
}