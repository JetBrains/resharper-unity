using System.Collections.Generic;
using JetBrains.Application.Parts;
using JetBrains.IDE;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Core.Feature.Services.Context;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Feature.Services.Technologies;

[SolutionComponent(Instantiation.DemandAnyThreadSafe)]
public class UnityProjectTechnologyDetailsProvider(UnitySolutionTracker unitySolutionTracker, UnitySolutionInformation unitySolutionInformation)
    : UnityProjectTechnologyProviderBase(unitySolutionTracker, unitySolutionInformation),
        IProjectTechnologyDetailsProvider
{
    IEnumerable<string> IProjectTechnologyDetailsProvider.GetProjectTechnology(IProject project)
    {
        foreach (var s in GetProjectTechnologyInternal())
            yield return s;
            
        if (myUnitySolutionTracker.HasUnityReference.Value)
            yield return $"Unity version: {myUnitySolutionInformation.GetUnityVersion()}";
    }
}