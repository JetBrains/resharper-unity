using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Settings;
using JetBrains.ProjectModel;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem
{
    [SolutionComponent]
    public class UnityProblemAnalyzerContextSettingsManager
    {
        private readonly IReadOnlyList<IUnityProblemAnalyzerContextSettingProvider> myProviders;

        public UnityProblemAnalyzerContextSettingsManager(
            IEnumerable<IUnityProblemAnalyzerContextSettingProvider> providers)
        {
            myProviders = providers.ToList();

            myProviders.AssertClassifications();
        }

        public IReadOnlyList<UnityProblemAnalyzerContextSetting> GetSettings(
            IContextBoundSettingsStore settingsStore)
        {
            var result = new List<UnityProblemAnalyzerContextSetting>(myProviders.Count);

            foreach (var provider in myProviders)
            {
                result.Add(provider.CheckSettings(settingsStore));
            }

            return result;
        }
    }
}