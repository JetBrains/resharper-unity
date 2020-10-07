using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Host.Features.CodeInsights;
using JetBrains.ReSharper.Host.Platform.Icons;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders;
using JetBrains.ReSharper.Plugins.Unity.Feature.Caches;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.ReSharper.Plugins.Unity.Rider.CodeInsights;
using JetBrains.ReSharper.Plugins.Unity.Yaml;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.UnityEvents;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Rider.Model;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Highlightings.IconsProviders
{
    [SolutionComponent]
    public class RiderEventHandlerDetector : EventHandlerDetector
    {
        private readonly AssetIndexingSupport myAssetIndexingSupport;
        private readonly UnityCodeInsightProvider myCodeInsightProvider;
        private readonly UnityUsagesCodeVisionProvider myUsagesCodeVisionProvider;
        private readonly DeferredCacheController myDeferredCacheController;
        private readonly UnitySolutionTracker mySolutionTracker;
        private readonly ConnectionTracker myConnectionTracker;
        private readonly IconHost myIconHost;
        private readonly AssetSerializationMode myAssetSerializationMode;

        public RiderEventHandlerDetector(ISolution solution,
                                         IApplicationWideContextBoundSettingStore settingsStore,
                                         AssetIndexingSupport assetIndexingSupport,
                                         UnityEventsElementContainer unityEventsElementContainer,
                                         UnityCodeInsightProvider codeInsightProvider,
                                         UnityUsagesCodeVisionProvider usagesCodeVisionProvider,
                                         DeferredCacheController deferredCacheController,
                                         UnitySolutionTracker solutionTracker, ConnectionTracker connectionTracker,
                                         IconHost iconHost, AssetSerializationMode assetSerializationMode,
                                         UnityProblemAnalyzerContextSystem contextSystem)
            : base(solution, settingsStore, unityEventsElementContainer, contextSystem)
        {
            myAssetIndexingSupport = assetIndexingSupport;
            myCodeInsightProvider = codeInsightProvider;
            myUsagesCodeVisionProvider = usagesCodeVisionProvider;
            myDeferredCacheController = deferredCacheController;
            mySolutionTracker = solutionTracker;
            myConnectionTracker = connectionTracker;
            myIconHost = iconHost;
            myAssetSerializationMode = assetSerializationMode;
        }

        protected override void AddHighlighting(IHighlightingConsumer consumer, ICSharpDeclaration element, string text, string tooltip,
                                                DaemonProcessKind kind)
        {
            var iconId = element.HasHotIcon(ContextSystem, SettingsStore.BoundSettingsStore, kind)
                ? InsightUnityIcons.InsightHot.Id
                : InsightUnityIcons.InsightUnity.Id;

            if (RiderIconProviderUtil.IsCodeVisionEnabled(SettingsStore.BoundSettingsStore, myCodeInsightProvider.ProviderId,
                () => { base.AddHighlighting(consumer, element, text, tooltip, kind); }, out var useFallback))
            {
                if (!useFallback)
                {
                    consumer.AddImplicitConfigurableHighlighting(element);
                }

                IconModel iconModel = myIconHost.Transform(iconId);
                if (myAssetIndexingSupport.IsEnabled.Value && myAssetSerializationMode.IsForceText)
                {
                    if (myDeferredCacheController.IsProcessingFiles())
                        iconModel = myIconHost.Transform(CodeInsightsThemedIcons.InsightWait.Id);

                    if (!myDeferredCacheController.CompletedOnce.Value)
                        tooltip = "Usages in assets are not available during asset indexing";

                }

                if (!myAssetIndexingSupport.IsEnabled.Value || !myDeferredCacheController.CompletedOnce.Value|| !myAssetSerializationMode.IsForceText)
                {
                    myCodeInsightProvider.AddHighlighting(consumer, element, element.DeclaredElement, text,
                        tooltip, text, iconModel, GetActions(element),
                        RiderIconProviderUtil.GetExtraActions(mySolutionTracker, myConnectionTracker));
                }
                else
                {
                    var count = UnityEventsElementContainer.GetAssetUsagesCount(element.DeclaredElement, out var estimate);
                    myUsagesCodeVisionProvider.AddHighlighting(consumer, element, element.DeclaredElement, count,
                        "Click to view usages in assets", "Assets usages",estimate, iconModel);
                }
            }
        }
    }
}