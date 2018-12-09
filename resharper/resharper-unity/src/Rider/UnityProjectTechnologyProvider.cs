using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.IDE.UsageStatistics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [ShellComponent]
    public class UnityClassLibProjectTechnologyProvider : IProjectTechnologyProvider
    {
        private readonly UnitySolutionTracker myUnitySolutionTracker;
        private readonly UnityReferencesTracker myUnityReferencesTracker;

        public UnityClassLibProjectTechnologyProvider(UnitySolutionTracker unitySolutionTracker, UnityReferencesTracker unityReferencesTracker)
        {
            myUnitySolutionTracker = unitySolutionTracker;
            myUnityReferencesTracker = unityReferencesTracker;
        }

        public IEnumerable<string> GetProjectTechnology(IProject project)
        {
            if (myUnityReferencesTracker.HasUnityReference.Value) yield return "Unity";
        
            if (myUnitySolutionTracker.IsUnityGeneratedProject.Value) yield return "UnityGenerated";
            else if (myUnitySolutionTracker.IsUnityProject.Value) yield return "UnitySidecar";
            else if (myUnityReferencesTracker.HasUnityReference.Value) yield return "UnityClassLib";
        }
    }
}