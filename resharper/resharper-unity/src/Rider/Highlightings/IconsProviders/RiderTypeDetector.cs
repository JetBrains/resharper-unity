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
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetUsages;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Highlightings.IconsProviders
{
    [SolutionComponent]
    public class RiderTypeDetector : TypeDetector
    {
        private readonly UnityUsagesCodeVisionProvider myUsagesCodeVisionProvider;
        private readonly UnityCodeInsightProvider myCodeInsightProvider;
        private readonly AssetUsagesElementContainer myAssetUsagesElementContainer;
        private readonly DeferredCacheController myDeferredCacheController;
        private readonly UnitySolutionTracker mySolutionTracker;
        private readonly ConnectionTracker myConnectionTracker;
        private readonly IconHost myIconHost;
        private readonly UnityYamlSupport myUnityYamlSupport;
        private readonly AssetSerializationMode myAssetSerializationMode;

        public RiderTypeDetector(ISolution solution, SolutionAnalysisService swa, CallGraphSwaExtensionProvider callGraphSwaExtensionProvider, 
            SettingsStore settingsStore, PerformanceCriticalCodeCallGraphAnalyzer analyzer, UnityApi unityApi,
            UnityUsagesCodeVisionProvider usagesCodeVisionProvider, UnityCodeInsightProvider codeInsightProvider, AssetUsagesElementContainer assetUsagesElementContainer,
            DeferredCacheController deferredCacheController, UnitySolutionTracker solutionTracker, ConnectionTracker connectionTracker,
            IconHost iconHost, UnityYamlSupport unityYamlSupport, AssetSerializationMode assetSerializationMode)
            : base(solution, swa, callGraphSwaExtensionProvider, settingsStore, unityApi, analyzer)
        {
            myUsagesCodeVisionProvider = usagesCodeVisionProvider;
            myCodeInsightProvider = codeInsightProvider;
            myAssetUsagesElementContainer = assetUsagesElementContainer;
            myDeferredCacheController = deferredCacheController;
            mySolutionTracker = solutionTracker;
            myConnectionTracker = connectionTracker;
            myIconHost = iconHost;
            myUnityYamlSupport = unityYamlSupport;
            myAssetSerializationMode = assetSerializationMode;
        }

        protected override void AddMonoBehaviourHiglighting(IHighlightingConsumer consumer, IClassLikeDeclaration declaration, string text,
            string tooltip, DaemonProcessKind kind)
        {
            if (RiderIconProviderUtil.IsCodeVisionEnabled(Settings, myCodeInsightProvider.ProviderId,
                () => { base.AddHighlighting(consumer, declaration, text, tooltip, kind); }, out var useFallback))
            {
                if (!useFallback)
                {
                    consumer.AddImplicitConfigurableHighlighting(declaration);
                }

                if (!myAssetSerializationMode.IsForceText)
                {
                    myCodeInsightProvider.AddHighlighting(consumer, declaration, declaration.DeclaredElement, text,
                        tooltip, text, myIconHost.Transform(InsightUnityIcons.InsightUnity.Id), GetActions(declaration),
                        RiderIconProviderUtil.GetExtraActions(mySolutionTracker, myConnectionTracker));
                }
                else
                {
                    if (!myDeferredCacheController.CompletedOnce.Value)
                    {
                        myCodeInsightProvider.AddHighlighting(consumer, declaration, declaration.DeclaredElement, text,
                            tooltip, text, myIconHost.Transform(CodeInsightsThemedIcons.InsightWait.Id), GetActions(declaration),
                            RiderIconProviderUtil.GetExtraActions(mySolutionTracker, myConnectionTracker));   
                    } else if (myDeferredCacheController.IsProcessingFiles())
                    {
                        var count = myAssetUsagesElementContainer.GetUsagesCount(declaration);
                        myUsagesCodeVisionProvider.AddHighlighting(consumer, declaration, declaration.DeclaredElement, count,
                            "Click to see usages in assets", "Assets usages", myIconHost.Transform(CodeInsightsThemedIcons.InsightWait.Id));
                    }
                    else
                    {
                        var count = myAssetUsagesElementContainer.GetUsagesCount(declaration);
                        myUsagesCodeVisionProvider.AddHighlighting(consumer, declaration, declaration.DeclaredElement, count,
                            "Click to see usages in assets", "Assets usages", myIconHost.Transform(InsightUnityIcons.InsightUnity.Id));
                    }
                }
            }
        }

        protected override void AddHighlighting(IHighlightingConsumer consumer, ICSharpDeclaration element, string text,
            string tooltip,
            DaemonProcessKind kind)
        {
            if (RiderIconProviderUtil.IsCodeVisionEnabled(Settings, myCodeInsightProvider.ProviderId,
                () => { base.AddHighlighting(consumer, element, text, tooltip, kind); }, out var useFallback))
            {
                if (!useFallback)
                {
                    consumer.AddImplicitConfigurableHighlighting(element);
                }
                myCodeInsightProvider.AddHighlighting(consumer, element, element.DeclaredElement, text,
                    tooltip, text, myIconHost.Transform(InsightUnityIcons.InsightUnity.Id), GetActions(element),
                    RiderIconProviderUtil.GetExtraActions(mySolutionTracker, myConnectionTracker));
            }
        }
    }
}