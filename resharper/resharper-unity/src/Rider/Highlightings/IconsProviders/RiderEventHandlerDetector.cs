using JetBrains.Application.Settings.Implementation;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
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
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetMethods;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Highlightings.IconsProviders
{
    [SolutionComponent]
    public class RiderEventHandlerDetector : EventHandlerDetector
    {
        private readonly AssetMethodsElementContainer myAssetMethodsElementContainer;
        private readonly UnityCodeInsightProvider myCodeInsightProvider;
        private readonly UnityUsagesCodeVisionProvider myUsagesCodeVisionProvider;
        private readonly DeferredCacheController myDeferredCacheController;
        private readonly UnitySolutionTracker mySolutionTracker;
        private readonly ConnectionTracker myConnectionTracker;
        private readonly IconHost myIconHost;
        private readonly AssetSerializationMode myAssetSerializationMode;

        public RiderEventHandlerDetector(ISolution solution, SolutionAnalysisService swa, CallGraphSwaExtensionProvider callGraphSwaExtensionProvider, 
            SettingsStore settingsStore, PerformanceCriticalCodeCallGraphAnalyzer analyzer,AssetMethodsElementContainer assetMethodsElementContainer,
            UnityCodeInsightProvider codeInsightProvider, UnityUsagesCodeVisionProvider usagesCodeVisionProvider, DeferredCacheController deferredCacheController,
            UnitySolutionTracker solutionTracker, ConnectionTracker connectionTracker,
            IconHost iconHost, AssetSerializationMode assetSerializationMode)
            : base(solution, swa,  settingsStore, callGraphSwaExtensionProvider, assetMethodsElementContainer, analyzer)
        {
            myAssetMethodsElementContainer = assetMethodsElementContainer;
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
            var iconId = element.HasHotIcon(Swa, CallGraphSwaExtensionProvider, Settings, Analyzer, kind)
                ? InsightUnityIcons.InsightHot.Id
                : InsightUnityIcons.InsightUnity.Id;
            
            if (RiderIconProviderUtil.IsCodeVisionEnabled(Settings, myCodeInsightProvider.ProviderId,
                () => { base.AddHighlighting(consumer, element, text, tooltip, kind); }, out var useFallback))
            {
                if (!useFallback)
                {
                    consumer.AddImplicitConfigurableHighlighting(element);
                }

                if (!myAssetSerializationMode.IsForceText)
                {
                    myCodeInsightProvider.AddHighlighting(consumer, element, element.DeclaredElement, text,
                        tooltip, text, myIconHost.Transform(iconId), GetActions(element),
                        RiderIconProviderUtil.GetExtraActions(mySolutionTracker, myConnectionTracker));
                }
                else
                {
                    if (!myDeferredCacheController.CompletedOnce.Value)
                    {
                        myCodeInsightProvider.AddHighlighting(consumer, element, element.DeclaredElement, text,
                            tooltip, text, myIconHost.Transform(CodeInsightsThemedIcons.InsightWait.Id), GetActions(element),
                            RiderIconProviderUtil.GetExtraActions(mySolutionTracker, myConnectionTracker));
                    } else if (myDeferredCacheController.IsProcessingFiles())
                    {
                        var count = myAssetMethodsElementContainer.GetAssetUsagesCount(element.DeclaredElement, out var estimate);
                        myUsagesCodeVisionProvider.AddHighlighting(consumer, element, element.DeclaredElement, count,
                            "Click to see usages in assets", "Assets usages", estimate, myIconHost.Transform(CodeInsightsThemedIcons.InsightWait.Id));
                    }
                    else
                    {
                        var count = myAssetMethodsElementContainer.GetAssetUsagesCount(element.DeclaredElement, out var estimate);
                        myUsagesCodeVisionProvider.AddHighlighting(consumer, element, element.DeclaredElement, count,
                            "Click to see usages in assets", "Assets usages",estimate, myIconHost.Transform(iconId));
                    }
                }
            }
        }
    }
}