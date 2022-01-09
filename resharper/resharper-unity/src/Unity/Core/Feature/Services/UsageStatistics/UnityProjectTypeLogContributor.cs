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
        private readonly UnityReferencesTracker myUnityReferencesTracker;

        public UnityProjectTypeLogContributor(UnitySolutionTracker unitySolutionTracker,
                                              UnityReferencesTracker unityReferencesTracker)
        {
            myUnitySolutionTracker = unitySolutionTracker;
            myUnityReferencesTracker = unityReferencesTracker;
        }

        public void ProcessSolutionStatistics([NotNull] JObject log)
        {
            if (myUnitySolutionTracker.IsUnityGeneratedProject.Value)
                log["unity_pt"] = "UnityGenerated";
            else if (myUnitySolutionTracker.IsUnityProject.Value)
                log["unity_pt"] = "UnitySidecar";
            else if (myUnityReferencesTracker.HasUnityReference.Value)
                log["unity_pt"] = "UnityClassLib";
        }
    }
}