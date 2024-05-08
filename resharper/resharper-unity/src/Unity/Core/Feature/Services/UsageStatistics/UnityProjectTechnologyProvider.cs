using System.Collections.Generic;
using JetBrains.Application.Parts;
using JetBrains.IDE.UsageStatistics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Feature.Services.UsageStatistics
{
    [SolutionComponent(Instantiation.DemandAnyThreadSafe)]
    public class UnityProjectTechnologyProvider : IProjectTechnologyProvider
    {
        private readonly UnitySolutionTracker myUnitySolutionTracker;

        public UnityProjectTechnologyProvider(UnitySolutionTracker unitySolutionTracker)
        {
            myUnitySolutionTracker = unitySolutionTracker;
        }

        public IEnumerable<string> GetProjectTechnology(IProject project)
        {
            if (myUnitySolutionTracker.HasUnityReference.Value)
            {
                yield return "Unity";
                yield return "GameDev";
            }
        }
    }
}