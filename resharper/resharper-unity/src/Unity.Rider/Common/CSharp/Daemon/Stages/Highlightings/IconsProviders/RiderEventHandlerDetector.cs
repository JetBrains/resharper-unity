using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Caches;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Core.Psi.Modules;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.ReSharper.Plugins.Unity.Rider.Common.CSharp.Daemon.CodeInsights;
using JetBrains.ReSharper.Plugins.Unity.Rider.Common.Protocol;
using JetBrains.ReSharper.Plugins.Unity.Yaml;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Anim.Explicit;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Anim.Implicit;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.UnityEvents;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Rider.Backend.Platform.Icons;
using JetBrains.Rider.Model;
using Strings = JetBrains.ReSharper.Plugins.Unity.Rider.Resources.Strings;

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
        private readonly AnimExplicitUsagesContainer myAnimExplicitUsagesContainer;
        [NotNull] private readonly AnimImplicitUsagesContainer myAnimImplicitUsagesContainer;

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
                                         [NotNull] AnimExplicitUsagesContainer animExplicitUsagesContainer,
                                         [NotNull] AnimImplicitUsagesContainer animImplicitUsagesContainer)
            : base(solution, settingsStore, unityEventsElementContainer, contextProvider, animExplicitUsagesContainer, animImplicitUsagesContainer)
        {
            myAssetIndexingSupport = assetIndexingSupport;
            myCodeInsightProvider = codeInsightProvider;
            myUsagesCodeVisionProvider = usagesCodeVisionProvider;
            myDeferredCacheController = deferredCacheController;
            mySolutionTracker = solutionTracker;
            myBackendUnityHost = backendUnityHost;
            myIconHost = iconHost;
            myAssetSerializationMode = assetSerializationMode;
            myAnimExplicitUsagesContainer = animExplicitUsagesContainer;
            myAnimImplicitUsagesContainer = animImplicitUsagesContainer;
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
                        tooltip = Strings.UsagesInAssetsAreNotAvailableDuring_Text;
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
            var animationEventUsagesCount = myAnimExplicitUsagesContainer
                    .GetEventUsagesCountFor(declaredElement, out var animationEventsEstimatedResult)
                    + myAnimImplicitUsagesContainer.GetEventUsagesCountFor(declaredElement,
                     out var animImplicitEstimatedResult);
            myUsagesCodeVisionProvider.AddHighlighting(consumer, element, declaredElement,
                animationEventUsagesCount + eventsCount, Strings.RiderEventHandlerDetector_AddEventsHighlighting_Click_to_view_usages_in_assets, Strings.RiderEventHandlerDetector_AddEventsHighlighting_Assets_usages,
                unityEventsEstimatedResult || animationEventsEstimatedResult || animImplicitEstimatedResult, iconModel);
        }
    }
}