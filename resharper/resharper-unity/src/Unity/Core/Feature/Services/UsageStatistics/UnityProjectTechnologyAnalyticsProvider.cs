#nullable enable
using System.Collections.Generic;
using JetBrains.Application.Parts;
using JetBrains.IDE.UsageStatistics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Feature.Services.UsageStatistics
{
    [SolutionComponent(Instantiation.DemandAnyThreadSafe)]
    public class UnityProjectTechnologyAnalyticsProvider(UnitySolutionTracker unitySolutionTracker, IUnityVersion unityVersion) : IProjectTechnologyAnalyticsProvider
    {
        public IEnumerable<string> GetProjectTechnology(IProject project)
        {
            if (unitySolutionTracker.HasUnityReference.Value)
            {
                yield return "Unity";
                yield return "GameDev";
            }
            
            var appPath = unityVersion.GetActualAppPathForSolution();
            if (UnityInstallationFinder.IsTuanjie(appPath))
                yield return "Tuanjie";
        }
    }
}