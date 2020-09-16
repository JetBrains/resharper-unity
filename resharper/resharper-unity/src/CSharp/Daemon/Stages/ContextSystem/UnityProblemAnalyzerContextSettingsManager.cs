using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Settings;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem
{
    [SolutionComponent]
    public class UnityProblemAnalyzerContextSettingsManager
    {
        private readonly List<IUnityProblemAnalyzerContextSettingProvider> myProviders;

        public UnityProblemAnalyzerContextSettingsManager(
            IEnumerable<IUnityProblemAnalyzerContextSettingProvider> providers)
        {
            myProviders = providers.ToList();

            myProviders.AssertClassifications();
        }

        public List<UnityProblemAnalyzerContextSetting> GetSettings(
            IContextBoundSettingsStore settingsStore)
        {
            var result = new List<UnityProblemAnalyzerContextSetting>(myProviders.Count);

            foreach (var provider in myProviders)
                result.Add(provider.CheckSettings(settingsStore));

            return result;
        }

        public UnityProblemAnalyzerContextSetting GetSettingForContext(IContextBoundSettingsStore settingsStore, UnityProblemAnalyzerContextElement contextElement)
        {
            foreach (var settingProvider in myProviders)
            {
                if (settingProvider.Context == contextElement)
                    return settingProvider.CheckSettings(settingsStore);
            }
            
            throw new KeyNotFoundException($"No such context: {contextElement}");
        }
    }
}