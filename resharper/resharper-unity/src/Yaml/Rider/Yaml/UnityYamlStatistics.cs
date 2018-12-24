using JetBrains.Platform.RdFramework.Util;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Rider;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules;
using JetBrains.UsageStatistics;
using Newtonsoft.Json.Linq;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Yaml
{
    [SolutionComponent]
    public class UnityYamlStatistics : IActivityLogContributorSolutionComponent
    {
        private readonly UnitySolutionTracker myUnitySolutionTracker;
        private readonly UnityExternalFilesModuleProcessor myModuleProcessor;

        public UnityYamlStatistics(UnitySolutionTracker unitySolutionTracker, UnityExternalFilesModuleProcessor moduleProcessor)
        {
            myUnitySolutionTracker = unitySolutionTracker;
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
        }
    }
}