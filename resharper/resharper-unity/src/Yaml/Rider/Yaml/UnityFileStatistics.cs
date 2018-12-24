using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules;
using JetBrains.UsageStatistics;
using Newtonsoft.Json.Linq;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Yaml
{
    [SolutionComponent]
    public class UnityFileStatistics : IActivityLogContributorSolutionComponent
    {
        private readonly ISolution mySolution;
        private readonly UnityYamlDisableStrategy myStrategy;

        public UnityFileStatistics(ISolution solution, UnityYamlDisableStrategy strategy)
        {
            mySolution = solution;
            myStrategy = strategy;
        }
        
        public void ProcessSolutionStatistics(JObject log)
        {
            var unityYamlStats = new JObject();
            log["UnityStats"] = unityYamlStats;
            unityYamlStats["Name"] = mySolution.Name.GetHashCode();
            unityYamlStats["Scenes"] = JArray.FromObject(myStrategy.SceneSizes);
            unityYamlStats["Prefabs"] = JArray.FromObject(myStrategy.PrefabSizes);
            unityYamlStats["Assets"] = JArray.FromObject(myStrategy.AssetSizes);
            unityYamlStats["TotalSize"] = myStrategy.TotalSize;
        }
    }
}