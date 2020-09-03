using JetBrains.Application.Settings;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.Settings;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.ContextSystem
{
    [SolutionComponent]
    public class PerformanceCriticalCodeContextSettingProvider : IUnityProblemAnalyzerContextSettingProvider
    {
        public UnityProblemAnalyzerContextElement Context => UnityProblemAnalyzerContextElement.PERFORMANCE_CONTEXT;
        public UnityProblemAnalyzerContextSetting CheckSettings(IContextBoundSettingsStore settingsStore)
        {
            var isPerformanceAnalysisEnabled =
                settingsStore.GetValue((UnitySettings s) => s.EnablePerformanceCriticalCodeHighlighting);
            
            return new UnityProblemAnalyzerContextSetting(isPerformanceAnalysisEnabled, UnityProblemAnalyzerContextElement.PERFORMANCE_CONTEXT);
        }
    }
}