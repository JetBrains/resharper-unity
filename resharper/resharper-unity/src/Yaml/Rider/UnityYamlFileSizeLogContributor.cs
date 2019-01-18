using JetBrains.Platform.RdFramework.Util;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Rider;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules;
using JetBrains.UsageStatistics;
using Newtonsoft.Json.Linq;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml
{
    [SolutionComponent]
    public class UnityYamlFileSizeLogContributor : IActivityLogContributorSolutionComponent
    {
        private readonly UnitySolutionTracker myUnitySolutionTracker;
        private readonly UnityYamlSupport myUnityYamlSupport;
        private readonly UnityExternalFilesModuleProcessor myModuleProcessor;

        public UnityYamlFileSizeLogContributor(UnitySolutionTracker unitySolutionTracker, UnityYamlSupport unityYamlSupport, UnityExternalFilesModuleProcessor moduleProcessor)
        {
            myUnitySolutionTracker = unitySolutionTracker;
            myUnityYamlSupport = unityYamlSupport;
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
            unityYamlStats["e"] = myUnityYamlSupport.IsUnityYamlParsingEnabled.Value;
        }
    }
}