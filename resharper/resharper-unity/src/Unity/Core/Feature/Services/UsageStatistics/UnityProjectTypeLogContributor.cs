using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.UsageStatistics;
using Newtonsoft.Json.Linq;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Feature.Services.UsageStatistics
{
    [SolutionComponent]
    public class UnityProjectTypeLogContributor : IActivityLogContributorSolutionComponent
    {
        private readonly UnitySolutionTracker myUnitySolutionTracker;
        
        public UnityProjectTypeLogContributor(UnitySolutionTracker unitySolutionTracker)
        {
            myUnitySolutionTracker = unitySolutionTracker;
        }

        public void ProcessSolutionStatistics([NotNull] JObject log)
        {
            if (myUnitySolutionTracker.IsUnityGeneratedProject.Value)
                log["unity_pt"] = "UnityGenerated";
            else if (myUnitySolutionTracker.IsUnityProject.Value)
                log["unity_pt"] = "UnitySidecar";
            else if (myUnitySolutionTracker.HasUnityReference.Value)
                log["unity_pt"] = "UnityClassLib";
        }
    }
}