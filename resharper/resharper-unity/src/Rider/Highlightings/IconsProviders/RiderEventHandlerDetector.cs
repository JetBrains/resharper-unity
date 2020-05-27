using JetBrains.Application.Settings.Implementation;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Host.Features.CodeInsights;
using JetBrains.ReSharper.Host.Platform.Icons;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.Feature.Caches;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.ReSharper.Plugins.Unity.Rider.CodeInsights;
using JetBrains.ReSharper.Plugins.Unity.Yaml;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.UnityEvents;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.Rider.Model;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Highlightings.IconsProviders
{
    [SolutionComponent]
    public class RiderEventHandlerDetector : EventHandlerDetector
    {
        private readonly AssetIndexingSupport myAssetIndexingSupport;
        private readonly UnityEventsElementContainer myUnityEventsElementContainer;
        private readonly UnityCodeInsightProvider myCodeInsightProvider;
        private readonly UnityUsagesCodeVisionProvider myUsagesCodeVisionProvider;
        private readonly DeferredCacheController myDeferredCacheController;
        private readonly UnitySolutionTracker mySolutionTracker;
        private readonly ConnectionTracker myConnectionTracker;
        private readonly IconHost myIconHost;
        private readonly IElementIdProvider myProvider;
        private readonly AssetSerializationMode myAssetSerializationMode;

        public RiderEventHandlerDetector(ISolution solution, CallGraphSwaExtensionProvider callGraphSwaExtensionProvider, 
            SettingsStore settingsStore, AssetIndexingSupport assetIndexingSupport, PerformanceCriticalCodeCallGraphMarksProvider marksProvider,UnityEventsElementContainer unityEventsElementContainer,
            UnityCodeInsightProvider codeInsightProvider, UnityUsagesCodeVisionProvider usagesCodeVisionProvider, DeferredCacheController deferredCacheController,
            UnitySolutionTracker solutionTracker, ConnectionTracker connectionTracker,
            IconHost iconHost, AssetSerializationMode assetSerializationMode, IElementIdProvider provider)
            : base(solution, settingsStore, callGraphSwaExtensionProvider, unityEventsElementContainer, marksProvider, provider)
        {
            myAssetIndexingSupport = assetIndexingSupport;
            myUnityEventsElementContainer = unityEventsElementContainer;
            myCodeInsightProvider = codeInsightProvider;
            myUsagesCodeVisionProvider = usagesCodeVisionProvider;
            myDeferredCacheController = deferredCacheController;
            mySolutionTracker = solutionTracker;
            myConnectionTracker = connectionTracker;
            myIconHost = iconHost;
            myProvider = provider;
            myAssetSerializationMode = assetSerializationMode;
        }

        protected override void AddHighlighting(IHighlightingConsumer consumer, ICSharpDeclaration element, string text, string tooltip,
            DaemonProcessKind kind)
        {
            var iconId = element.HasHotIcon(CallGraphSwaExtensionProvider, Settings, MarksProvider, kind, myProvider)
                ? InsightUnityIcons.InsightHot.Id
                : InsightUnityIcons.InsightUnity.Id;
            
            if (RiderIconProviderUtil.IsCodeVisionEnabled(Settings, myCodeInsightProvider.ProviderId,
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
                    var count = myUnityEventsElementContainer.GetAssetUsagesCount(element.DeclaredElement, out var estimate);
                    myUsagesCodeVisionProvider.AddHighlighting(consumer, element, element.DeclaredElement, count,
                        "Click to view usages in assets", "Assets usages",estimate, iconModel);
                }
            }
        }
    }
}