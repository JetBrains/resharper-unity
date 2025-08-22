using System.Collections.Generic;
using JetBrains.Application.Parts;
using JetBrains.IDE.UsageStatistics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Core.Feature.Services.Context;
using JetBrains.ReSharper.Plugins.Unity.Core.Feature.Services.Technologies;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Feature.Services.UsageStatistics;

[SolutionComponent(Instantiation.DemandAnyThreadSafe)]
public class UnityProjectTechnologyAnalyticsProvider(UnitySolutionTracker unitySolutionTracker, UnitySolutionInformation unitySolutionInformation)
    : UnityProjectTechnologyProviderBase(unitySolutionTracker, unitySolutionInformation),
        IProjectTechnologyProvider
{
    IEnumerable<string> IProjectTechnologyProvider.GetProjectTechnology(IProject project)
    {
        return GetProjectTechnologyInternal();
    }
}