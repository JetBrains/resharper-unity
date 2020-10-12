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
using JetBrains.ReSharper.Plugins.Unity.Rider.Protocol;
using JetBrains.ReSharper.Plugins.Unity.Yaml;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetUsages;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Rider.Model;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Highlightings.IconsProviders
{
    [SolutionComponent]
    public class RiderTypeDetector : TypeDetector
    {
        private readonly AssetIndexingSupport myAssetIndexingSupport;
        private readonly UnityUsagesCodeVisionProvider myUsagesCodeVisionProvider;
        private readonly UnityCodeInsightProvider myCodeInsightProvider;
        private readonly AssetScriptUsagesElementContainer myAssetScriptUsagesElementContainer;
        private readonly DeferredCacheController myDeferredCacheController;
        private readonly UnitySolutionTracker mySolutionTracker;
        private readonly UnityEditorStateHost myUnityEditorStateHost;
        private readonly IconHost myIconHost;
        private readonly AssetSerializationMode myAssetSerializationMode;

        public RiderTypeDetector(ISolution solution, CallGraphSwaExtensionProvider callGraphSwaExtensionProvider,
                                 IApplicationWideContextBoundSettingStore settingsStore,
                                 PerformanceCriticalCodeCallGraphMarksProvider marksProvider, UnityApi unityApi,
                                 AssetIndexingSupport assetIndexingSupport,
                                 UnityUsagesCodeVisionProvider usagesCodeVisionProvider,
                                 UnityCodeInsightProvider codeInsightProvider,
                                 AssetScriptUsagesElementContainer assetScriptUsagesElementContainer,
                                 DeferredCacheController deferredCacheController, UnitySolutionTracker solutionTracker,
                                 UnityEditorStateHost unityEditorStateHost,
                                 IconHost iconHost, AssetSerializationMode assetSerializationMode,
                                 IElementIdProvider elementIdProvider)
            : base(solution, callGraphSwaExtensionProvider, settingsStore, unityApi, marksProvider, elementIdProvider)
        {
            myAssetIndexingSupport = assetIndexingSupport;
            myUsagesCodeVisionProvider = usagesCodeVisionProvider;
            myCodeInsightProvider = codeInsightProvider;
            myAssetScriptUsagesElementContainer = assetScriptUsagesElementContainer;
            myDeferredCacheController = deferredCacheController;
            mySolutionTracker = solutionTracker;
            myUnityEditorStateHost = unityEditorStateHost;
            myIconHost = iconHost;
            myAssetSerializationMode = assetSerializationMode;
        }

        protected override void AddMonoBehaviourHighlighting(IHighlightingConsumer consumer, IClassLikeDeclaration declaration, string text,
                                                             string tooltip, DaemonProcessKind kind)
        {
            if (RiderIconProviderUtil.IsCodeVisionEnabled(SettingsStore.BoundSettingsStore, myCodeInsightProvider.ProviderId,
                () => { base.AddHighlighting(consumer, declaration, text, tooltip, kind); }, out var useFallback))
            {
                if (!useFallback)
                {
                    consumer.AddImplicitConfigurableHighlighting(declaration);
                }

                IconModel iconModel = myIconHost.Transform(InsightUnityIcons.InsightUnity.Id);
                if (myAssetIndexingSupport.IsEnabled.Value && myAssetSerializationMode.IsForceText)
                {
                    if (myDeferredCacheController.IsProcessingFiles())
                        iconModel = myIconHost.Transform(CodeInsightsThemedIcons.InsightWait.Id);

                    if (!myDeferredCacheController.CompletedOnce.Value)
                        tooltip = "Usages in assets are not available during asset indexing";
                }

                if (!myAssetIndexingSupport.IsEnabled.Value ||!myDeferredCacheController.CompletedOnce.Value || !myAssetSerializationMode.IsForceText)
                {
                    myCodeInsightProvider.AddHighlighting(consumer, declaration, declaration.DeclaredElement, text,
                        tooltip, text, iconModel, GetActions(declaration),
                        RiderIconProviderUtil.GetExtraActions(mySolutionTracker, myUnityEditorStateHost));
                }
                else
                {
                    var count = myAssetScriptUsagesElementContainer.GetUsagesCount(declaration, out var estimatedResult);
                    myUsagesCodeVisionProvider.AddHighlighting(consumer, declaration, declaration.DeclaredElement, count,
                        "Click to view usages in assets", "Assets usages", estimatedResult, iconModel);
                }
            }
        }

        protected override void AddHighlighting(IHighlightingConsumer consumer, ICSharpDeclaration element, string text,
            string tooltip,
            DaemonProcessKind kind)
        {
            if (RiderIconProviderUtil.IsCodeVisionEnabled(SettingsStore.BoundSettingsStore, myCodeInsightProvider.ProviderId,
                () => { base.AddHighlighting(consumer, element, text, tooltip, kind); }, out var useFallback))
            {
                if (!useFallback)
                {
                    consumer.AddImplicitConfigurableHighlighting(element);
                }
                myCodeInsightProvider.AddHighlighting(consumer, element, element.DeclaredElement, text,
                    tooltip, text, myIconHost.Transform(InsightUnityIcons.InsightUnity.Id), GetActions(element),
                    RiderIconProviderUtil.GetExtraActions(mySolutionTracker, myUnityEditorStateHost));
            }
        }
    }
}