using JetBrains.Platform.RdFramework.Util;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis
{
    [SolutionComponent]
    public class UnityCallGraphSuppressor : ICallGraphSuppressor
    {
        private readonly bool myAvailable;

        public UnityCallGraphSuppressor(UnitySolutionTracker tracker)
        {
            myAvailable = tracker.IsUnityProject.HasTrueValue();
        }

        public int GetPriority() => 10;

        public bool IsAvailable() => myAvailable;
    }
}