using System;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Daemon.SolutionAnalysis;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;

namespace JetBrains.ReSharper.Plugins.Tests.Unity
{
    public static class UnityProjectCookie
    {
        public static IDisposable RunUnitySolutionCookie(ISolution solution)
        {
            var myUnitySolutionTracker = solution.GetComponent<UnitySolutionTracker>();
            var myOriginalValue = myUnitySolutionTracker.HasUnityReference.Value;
            myUnitySolutionTracker.HasUnityReference.Value = true;
            return Disposable.CreateAction(() => myUnitySolutionTracker.HasUnityReference.Value = myOriginalValue);
        }
    }
}