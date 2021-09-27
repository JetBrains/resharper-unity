using JetBrains.Collections.Viewable;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Core.Psi.Modules;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.UsageStatistics;
using Newtonsoft.Json.Linq;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Feature.Services.UsageStatistics
{
    [SolutionComponent]
    public class UnityExternalFilesFileSizeLogContributor : IActivityLogContributorSolutionComponent
    {
        private readonly UnitySolutionTracker myUnitySolutionTracker;
        private readonly AssetIndexingSupport myAssetIndexingSupport;
        private readonly UnityExternalFilesModuleProcessor myModuleProcessor;

        public UnityExternalFilesFileSizeLogContributor(UnitySolutionTracker unitySolutionTracker,
                                                        AssetIndexingSupport assetIndexingSupport,
                                                        UnityExternalFilesModuleProcessor moduleProcessor)
        {
            myUnitySolutionTracker = unitySolutionTracker;
            myAssetIndexingSupport = assetIndexingSupport;
            myModuleProcessor = moduleProcessor;
        }

        public void ProcessSolutionStatistics(JObject log)
        {
            if (!myUnitySolutionTracker.IsUnityProject.HasTrueValue())
                return;

            if (myModuleProcessor.SceneSizes.Count == 0 && myModuleProcessor.PrefabSizes.Count == 0 && myModuleProcessor.AssetSizes.Count == 0)
                return;

            var stats = new JObject();
            log["uys"] = stats;
            stats["s"] = JArray.FromObject(myModuleProcessor.SceneSizes);
            stats["p"] = JArray.FromObject(myModuleProcessor.PrefabSizes);
            stats["a"] = JArray.FromObject(myModuleProcessor.AssetSizes);
            stats["kba"] = JArray.FromObject(myModuleProcessor.KnownBinaryAssetSizes);
            stats["ebna"] = JArray.FromObject(myModuleProcessor.ExcludedByNameAssetsSizes);
            stats["e"] = myAssetIndexingSupport.IsEnabled.Value;
        }
    }
}