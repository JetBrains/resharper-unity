using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Caches;
using JetBrains.ReSharper.Plugins.Unity.Rider.Protocol;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules
{
    [SolutionComponent]
    public class RiderUnityYamlDisableStrategy : UnityYamlDisableStrategy
    {
        private readonly FrontendBackendHost myFrontendBackendHost;

        public RiderUnityYamlDisableStrategy(Lifetime lifetime, ISolution solution, SolutionCaches solutionCaches,
                                             IApplicationWideContextBoundSettingStore settingsStore,
                                             AssetIndexingSupport assetIndexingSupport, FrontendBackendHost frontendBackendHost)
            : base(lifetime, solution, solutionCaches, settingsStore, assetIndexingSupport)
        {
            myFrontendBackendHost = frontendBackendHost;

            myFrontendBackendHost.Do(t =>
                t.EnableYamlParsing.Advise(lifetime, _ => assetIndexingSupport.IsEnabled.Value = true));
        }

        protected override void NotifyYamlParsingDisabled()
        {
            myFrontendBackendHost.Do(t => t.NotifyYamlHugeFiles());
        }
    }
}