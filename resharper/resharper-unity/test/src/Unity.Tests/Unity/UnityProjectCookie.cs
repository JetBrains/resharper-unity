using System;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;

namespace JetBrains.ReSharper.Plugins.Tests.Unity
{
    public static class UnityProjectCookie
    {
        public static IDisposable RunUnitySolutionCookie(ISolution solution)
        {
            var unitySolutionTracker = solution.GetComponent<UnitySolutionTracker>();
            var originalValue = unitySolutionTracker.HasUnityReference.Value;
            unitySolutionTracker.HasUnityReference.Value = true;
            return Disposable.CreateAction(() => unitySolutionTracker.HasUnityReference.Value = originalValue);
        }
    }
}