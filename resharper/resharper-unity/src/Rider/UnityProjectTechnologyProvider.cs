using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.IDE.UsageStatistics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [ShellComponent]
    public class UnityProjectTechnologyProvider : IProjectTechnologyProvider
    {
        private readonly UnitySolutionTracker myUnitySolutionTracker;

        public UnityProjectTechnologyProvider(UnitySolutionTracker unitySolutionTracker)
        {
            myUnitySolutionTracker = unitySolutionTracker;
        }
        
        public IEnumerable<string> GetProjectTechnology(IProject project)
        {
            if (myUnitySolutionTracker.IsUnityGeneratedProject.Value) yield return "Unity";      
        }
    }
    
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
            if (myUnityReferencesTracker.HasUnityReference.Value && !myUnitySolutionTracker.IsUnityProject.Value) 
                yield return "UnityClassLib";      
        }
    }
}