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
            if (unitySolutionTracker.IsUnityGeneratedProject.Value)
                yield return "UnityGenerated";
            else if (unitySolutionTracker.IsUnityProject.Value)
                yield return "UnitySidecar";
            else if (unityReferencesTracker.HasUnityReference.Value)
                yield return "UnityClassLib";
        }
    }
}