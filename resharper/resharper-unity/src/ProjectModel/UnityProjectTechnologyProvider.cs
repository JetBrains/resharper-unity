using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.IDE.UsageStatistics;
using JetBrains.ProjectModel;

namespace JetBrains.ReSharper.Plugins.Unity.ProjectModel
{
    [ShellComponent]
    public class UnityProjectTechnologyProvider : IProjectTechnologyProvider
    {
        public IEnumerable<string> GetProjectTechnology(IProject project)
        {
            var unitySolutionTracker = project.GetSolution().GetComponent<UnitySolutionTracker>();
            var unityReferencesTracker = project.GetSolution().GetComponent<UnityReferencesTracker>();
            if (unitySolutionTracker.IsUnityGeneratedProject.Maybe.Value)
                yield return "UnityGenerated";
            if (unitySolutionTracker.IsUnityProject.Maybe.Value)
                yield return "UnitySidecar";
            if (unityReferencesTracker.HasUnityReference.Maybe.Value)
                yield return "UnitySidecar";
        }
    }
}