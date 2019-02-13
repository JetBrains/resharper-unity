using JetBrains.Platform.RdFramework.Util;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis
{
    [SolutionComponent]
    public class UnityTestCallGraphSuppressor : ICallGraphSuppressor
    {
        public UnityTestCallGraphSuppressor()
        {
        }

        public int GetPriority() => 20;

        public bool IsAvailable() => true;
    }
}