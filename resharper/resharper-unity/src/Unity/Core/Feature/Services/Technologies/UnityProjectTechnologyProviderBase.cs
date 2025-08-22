using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.Unity.Core.Feature.Services.Context;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Feature.Services.Technologies;

public abstract class UnityProjectTechnologyProviderBase(UnitySolutionTracker unitySolutionTracker, UnitySolutionInformation unitySolutionInformation)
{
    protected readonly UnitySolutionInformation myUnitySolutionInformation = unitySolutionInformation;
    protected readonly UnitySolutionTracker myUnitySolutionTracker = unitySolutionTracker;

    protected IEnumerable<string> GetProjectTechnologyInternal()
    {
        if (!myUnitySolutionTracker.HasUnityReference.Value) 
            yield break;
        yield return "GameDev";
        yield return "Unity";
    }
}