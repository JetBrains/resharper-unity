using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.Rd.Base;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;

namespace JetBrains.ReSharper.Plugins.Tests.UnityTestComponents;

[SolutionComponent(InstantiationEx.LegacyDefault)]
public class UnitySolutionTrackerInitializerForTests
{
    public UnitySolutionTrackerInitializerForTests(UnitySolutionTracker unitySolutionTracker)
    {
        unitySolutionTracker.IsUnityProjectFolder.SetValue(true);
        unitySolutionTracker.IsUnityProject.SetValue(true);
    }
}