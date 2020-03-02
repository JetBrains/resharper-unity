using JetBrains.Collections.Viewable;
using JetBrains.Platform.RdFramework.Util;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules;
using JetBrains.UsageStatistics;
using Newtonsoft.Json.Linq;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Feature.Services.UsageStatistics
{
    [SolutionComponent]
    public class UnityYamlFileSizeLogContributor : IActivityLogContributorSolutionComponent
    {
        private readonly UnitySolutionTracker myUnitySolutionTracker;
        private readonly AssetIndexingSupport myAssetIndexingSupport;
        private readonly UnityExternalFilesModuleProcessor myModuleProcessor;

        public UnityYamlFileSizeLogContributor(UnitySolutionTracker unitySolutionTracker, AssetIndexingSupport assetIndexingSupport, UnityExternalFilesModuleProcessor moduleProcessor)
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

            var unityYamlStats = new JObject();
            log["uys"] = unityYamlStats;
            unityYamlStats["s"] = JArray.FromObject(myModuleProcessor.SceneSizes);
            unityYamlStats["p"] = JArray.FromObject(myModuleProcessor.PrefabSizes);
            unityYamlStats["a"] = JArray.FromObject(myModuleProcessor.AssetSizes);
            unityYamlStats["kba"] = JArray.FromObject(myModuleProcessor.KnownBinaryAssetSizes);
            unityYamlStats["ebna"] = JArray.FromObject(myModuleProcessor.ExcludedByNameAssetsSizes);
            unityYamlStats["e"] = myAssetIndexingSupport.IsEnabled.Value;
        }
    }
}