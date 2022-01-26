using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Caches;
using JetBrains.ReSharper.Plugins.Unity.Core.Psi.Modules;
using JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Protocol;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Core.Psi.Modules
{
    [SolutionComponent]
    public class RiderUnityExternalFilesIndexDisablingStrategy : UnityExternalFilesIndexDisablingStrategy
    {
        private readonly FrontendBackendHost myFrontendBackendHost;

        public RiderUnityExternalFilesIndexDisablingStrategy(Lifetime lifetime,
                                                             SolutionCaches solutionCaches,
                                                             IApplicationWideContextBoundSettingStore settingsStore,
                                                             AssetIndexingSupport assetIndexingSupport,
                                                             FrontendBackendHost frontendBackendHost)
            : base(solutionCaches, settingsStore, assetIndexingSupport)
        {
            myFrontendBackendHost = frontendBackendHost;

            myFrontendBackendHost.Do(t =>
                t.EnableYamlParsing.Advise(lifetime, _ => assetIndexingSupport.IsEnabled.Value = true));
        }

        protected override void NotifyAssetIndexingDisabled()
        {
            myFrontendBackendHost.Do(t => t.NotifyYamlHugeFiles());
        }
    }
}