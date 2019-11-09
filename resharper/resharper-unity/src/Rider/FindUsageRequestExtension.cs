using System.Linq;
using JetBrains.Rider.Model;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    public static class FindUsageRequestExtension
    {
        public static RdFindUsageResultElement ConvertToUnityModel(this FindUsageResultElement request)
        {
            return new RdFindUsageResultElement(request.IsPrefab, request.ExpandInTreeView, request.FilePath, request.FileName, request.PathElements, request.RootIndices);
        }

        public static RdFindUsageResult ConvertToUnityModel(this FindUsageResult request)
        {
            return new RdFindUsageResult(request.Target, request.Elements.Select(t => t.ConvertToUnityModel()).ToArray());
        }
    }
}