using JetBrains.Application.Settings;
using JetBrains.ProjectModel;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem
{
    [SolutionComponent]
    public class UnityProblemAnalyzerContextSystem
    {
        private readonly UnityProblemAnalyzerContextManager myContextManager;
        private readonly UnityProblemAnalyzerContextSettingsManager mySettingsManager;

        public UnityProblemAnalyzerContextSystem(UnityProblemAnalyzerContextManager contextManager,
            UnityProblemAnalyzerContextSettingsManager settingsManager)
        {
            myContextManager = contextManager;
            mySettingsManager = settingsManager;
        }

        public UnityProblemAnalyzerContextManagerInstance GetManagerInstance(IContextBoundSettingsStore settingsStore)
        {
            var settings = mySettingsManager.GetSettings(settingsStore);
            var settingManagerInstance = myContextManager.GetInstance(settings);
            
            return settingManagerInstance;
        }
    }
}