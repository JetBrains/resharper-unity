using JetBrains.Annotations;
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

        [NotNull]
        public UnityProblemAnalyzerContextManagerInstance GetManagerInstance(IContextBoundSettingsStore settingsStore)
        {
            var settings = mySettingsManager.GetSettings(settingsStore);
            var settingManagerInstance = myContextManager.GetInstance(settings);
            
            return settingManagerInstance;
        }

        [NotNull]
        public IUnityProblemAnalyzerContextProvider GetContextProvider([NotNull] IContextBoundSettingsStore settingsStore, UnityProblemAnalyzerContextElement contextElement)
        {
            var setting = mySettingsManager.GetSettingForContext(settingsStore, contextElement);
            var contextProvider = myContextManager.GetContextProvider(setting);
            
            return contextProvider;
        }
    }
}