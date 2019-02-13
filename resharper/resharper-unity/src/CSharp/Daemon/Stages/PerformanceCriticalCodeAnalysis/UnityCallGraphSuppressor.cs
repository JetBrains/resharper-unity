using JetBrains.Application.Settings;
using JetBrains.Platform.RdFramework.Util;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Daemon.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Settings;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis
{
    [SolutionComponent]
    public class UnityCallGraphSuppressor : ICallGraphSuppressor
    {
        private readonly bool myAvailable;

        public UnityCallGraphSuppressor(ISolution solution, UnitySolutionTracker tracker, ISettingsStore settingsStore)
        {
            var settings = settingsStore.BindToContextTransient(ContextRange.Smart(solution.ToDataContext()));
            var enabled = settings.GetValue((UnitySettings s) => s.EnablePerformanceCriticalCodeHighlighting);
            myAvailable = enabled && tracker.IsUnityProject.HasTrueValue();
        }

        public int GetPriority() => 10;

        public bool IsAvailable() => myAvailable;
    }
}