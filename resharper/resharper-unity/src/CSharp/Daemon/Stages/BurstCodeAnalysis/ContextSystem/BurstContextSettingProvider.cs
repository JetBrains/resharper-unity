using JetBrains.Application.Settings;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.Settings;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.ContextSystem
{
    [SolutionComponent]
    public class BurstContextSettingProvider : IUnityProblemAnalyzerContextSettingProvider
    {
        public UnityProblemAnalyzerContextElement Context => UnityProblemAnalyzerContextElement.BURST_CONTEXT;

        public UnityProblemAnalyzerContextSetting CheckSettings(IContextBoundSettingsStore settingsStore)
        {
            var isBurstAvailable = settingsStore.GetValue((UnitySettings s) => s.EnableBurstCodeHighlighting);
            
            return new UnityProblemAnalyzerContextSetting(isBurstAvailable,
                UnityProblemAnalyzerContextElement.BURST_CONTEXT);
        }
    }
}