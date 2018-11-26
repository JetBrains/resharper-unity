using JetBrains.Platform.Unity.EditorPluginModel;
using JetBrains.Rider.Model;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    public static class FindUsageRequestExtension
    {
        public static RdFindUsageResult ConvertToUnityModel(this FindUsageResult request)
        {
            return new RdFindUsageResult(request.IsPrefab, request.ExpandInTreeView, request.FilePath, request.FileName, request.PathElements, request.RootIndices);
        }
    }
}