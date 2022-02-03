using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.ReSharper.Plugins.Unity.Core.Feature.Caches;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Core.Psi.Modules;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.ReSharper.Plugins.Unity.Rider.Common.CSharp.Daemon.CodeInsights;
using JetBrains.ReSharper.Plugins.Unity.Rider.Common.Protocol;
using JetBrains.ReSharper.Plugins.Unity.Yaml;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AnimationEventsUsages;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.UnityEvents;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Rider.Backend.Platform.Icons;
using JetBrains.Rider.Model;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Common.CSharp.Daemon.Stages.Highlightings.IconsProviders
{
    [SolutionComponent]
    public class RiderEventHandlerDetector : EventHandlerDetector
    {
        private readonly AssetIndexingSupport myAssetIndexingSupport;
        private readonly UnityCodeInsightProvider myCodeInsightProvider;
        private readonly UnityUsagesCodeVisionProvider myUsagesCodeVisionProvider;
        private readonly DeferredCacheController myDeferredCacheController;
        private readonly UnitySolutionTracker mySolutionTracker;
        private readonly IBackendUnityHost myBackendUnityHost;
        private readonly IconHost myIconHost;
        private readonly AssetSerializationMode myAssetSerializationMode;
        private readonly AnimationEventUsagesContainer myAnimationEventUsagesContainer;

        public RiderEventHandlerDetector(ISolution solution,
                                         IApplicationWideContextBoundSettingStore settingsStore,
                                         AssetIndexingSupport assetIndexingSupport,
                                         UnityEventsElementContainer unityEventsElementContainer,
                                         UnityCodeInsightProvider codeInsightProvider,
                                         UnityUsagesCodeVisionProvider usagesCodeVisionProvider,
                                         DeferredCacheController deferredCacheController,
                                         UnitySolutionTracker solutionTracker,
                                         IBackendUnityHost backendUnityHost,
                                         IconHost iconHost, AssetSerializationMode assetSerializationMode,
                                         PerformanceCriticalContextProvider contextProvider,
                                         [NotNull] AnimationEventUsagesContainer animationEventUsagesContainer)
            : base(solution, settingsStore, unityEventsElementContainer, contextProvider, animationEventUsagesContainer)
        {
            myAssetIndexingSupport = assetIndexingSupport;
            myCodeInsightProvider = codeInsightProvider;
            myUsagesCodeVisionProvider = usagesCodeVisionProvider;
            myDeferredCacheController = deferredCacheController;
            mySolutionTracker = solutionTracker;
            myBackendUnityHost = backendUnityHost;
            myIconHost = iconHost;
            myAssetSerializationMode = assetSerializationMode;
            myAnimationEventUsagesContainer = animationEventUsagesContainer;
        }

        protected override void AddHighlighting(IHighlightingConsumer consumer, ICSharpDeclaration element, string text, string tooltip,
                                                IReadOnlyCallGraphContext context)
        {
            var iconId = element.HasHotIcon(ContextProvider, SettingsStore.BoundSettingsStore, context)
                ? InsightUnityIcons.InsightHot.Id
                : InsightUnityIcons.InsightUnity.Id;

            if (RiderIconProviderUtil.IsCodeVisionEnabled(SettingsStore.BoundSettingsStore, myCodeInsightProvider.ProviderId,
                () => { base.AddHighlighting(consumer, element, text, tooltip, context); }, out var useFallback))
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

                if (!myAssetIndexingSupport.IsEnabled.Value || !myDeferredCacheController.CompletedOnce.Value || !myAssetSerializationMode.IsForceText)
                {
                    myCodeInsightProvider.AddHighlighting(consumer, element, element.DeclaredElement, text,
                        tooltip, text, iconModel, GetActions(element),
                        RiderIconProviderUtil.GetExtraActions(mySolutionTracker, myBackendUnityHost));
                }
                else
                {
                    AddEventsHighlighting(consumer, element, iconModel);
                }
            }
        }

        private void AddEventsHighlighting([NotNull] IHighlightingConsumer consumer,
                                           [NotNull] IDeclaration element,
                                           [NotNull] IconModel iconModel)
        {
            var declaredElement = element.DeclaredElement;
            var eventsCount = UnityEventsElementContainer.GetAssetUsagesCount(declaredElement, out var unityEventsEstimatedResult);
            var animationEventUsagesCount = myAnimationEventUsagesContainer
                .GetEventUsagesCountFor(declaredElement, out var animationEventsEstimatedResult);
            myUsagesCodeVisionProvider.AddHighlighting(consumer, element, declaredElement,
                animationEventUsagesCount + eventsCount, "Click to view usages in assets", "Assets usages",
                unityEventsEstimatedResult || animationEventsEstimatedResult, iconModel);
        }
    }
}