using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.IDE.UsageStatistics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Statistics
{
    [SolutionComponent]
    public class UnityProjectTechnologyProvider : IProjectTechnologyProvider
    {
        private readonly UnityReferencesTracker myUnityReferencesTracker;

        public UnityProjectTechnologyProvider(UnityReferencesTracker unityReferencesTracker)
        {
            myUnityReferencesTracker = unityReferencesTracker;
        }

        public IEnumerable<string> GetProjectTechnology(IProject project)
        {
            if (myUnityReferencesTracker.HasUnityReference.Value) yield return "Unity";
        }
    }
}