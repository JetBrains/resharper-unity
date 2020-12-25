using JetBrains.Application.Settings;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Host.Features.CodeInsights;
using JetBrains.ReSharper.Host.Platform.Icons;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.Feature.Caches;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.ReSharper.Plugins.Unity.Rider.CodeInsights;
using JetBrains.ReSharper.Plugins.Unity.Rider.Protocol;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Plugins.Unity.Yaml;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Highlightings.IconsProviders
{
    [SolutionComponent]
    public class RiderFieldDetector : FieldDetector
    {
        private readonly UnityCodeInsightFieldUsageProvider myFieldUsageProvider;
        private readonly DeferredCacheController myDeferredCacheController;
        private readonly UnitySolutionTracker mySolutionTracker;
        private readonly BackendUnityHost myBackendUnityHost;
        private readonly IconHost myIconHost;
        private readonly AssetSerializationMode myAssetSerializationMode;

        public RiderFieldDetector(ISolution solution,
                                  IApplicationWideContextBoundSettingStore settingsStore,
                                  UnityApi unityApi,
                                  UnityCodeInsightFieldUsageProvider fieldUsageProvider,
                                  DeferredCacheController deferredCacheController,
                                  UnitySolutionTracker solutionTracker,
                                  BackendUnityHost backendUnityHost,
                                  IconHost iconHost, AssetSerializationMode assetSerializationMode,
                                  PerformanceCriticalContextProvider contextProvider)
            : base(solution, settingsStore, unityApi, contextProvider)
        {
            myFieldUsageProvider = fieldUsageProvider;
            myDeferredCacheController = deferredCacheController;
            mySolutionTracker = solutionTracker;
            myBackendUnityHost = backendUnityHost;
            myIconHost = iconHost;
            myAssetSerializationMode = assetSerializationMode;
        }

        protected override void AddMonoBehaviourHighlighting(IHighlightingConsumer consumer, ICSharpDeclaration element,
                                                             string text,
                                                             string tooltip, IReadOnlyCallGraphContext context)
        {
            if (!myAssetSerializationMode.IsForceText ||
                !SettingsStore.BoundSettingsStore.GetValue((UnitySettings key) => key.EnableInspectorPropertiesEditor) ||
                !SettingsStore.BoundSettingsStore.GetValue((UnitySettings key) => key.IsAssetIndexingEnabled))
            {
                AddHighlighting(consumer, element, text, tooltip, context);
                return;
            }

            if (RiderIconProviderUtil.IsCodeVisionEnabled(SettingsStore.BoundSettingsStore, myFieldUsageProvider.ProviderId,
                () => { base.AddHighlighting(consumer, element, text, tooltip, context); }, out var useFallback))
            {
                if (!useFallback)
                {
                    consumer.AddImplicitConfigurableHighlighting(element);
                }

                var isProcessing = myDeferredCacheController.IsProcessingFiles();
                myFieldUsageProvider.AddInspectorHighlighting(consumer, element, element.DeclaredElement, text,
                    tooltip, isProcessing ? "Inspector values are not available during asset indexing" : text,
                    isProcessing ? myIconHost.Transform(CodeInsightsThemedIcons.InsightWait.Id) : myIconHost.Transform(InsightUnityIcons.InsightUnity.Id),
                    GetActions(element), RiderIconProviderUtil.GetExtraActions(mySolutionTracker, myBackendUnityHost));
            }
        }


        protected override void AddHighlighting(IHighlightingConsumer consumer, ICSharpDeclaration element, string text,
            string tooltip,
            IReadOnlyCallGraphContext context)
        {
            if (RiderIconProviderUtil.IsCodeVisionEnabled(SettingsStore.BoundSettingsStore, myFieldUsageProvider.ProviderId,
                () => { base.AddHighlighting(consumer, element, text, tooltip, context); }, out var useFallback))
            {
                if (!useFallback)
                {
                    consumer.AddImplicitConfigurableHighlighting(element);
                }
                myFieldUsageProvider.AddHighlighting(consumer, element, element.DeclaredElement, text,
                    tooltip, text, myIconHost.Transform(InsightUnityIcons.InsightUnity.Id), GetActions(element),
                    RiderIconProviderUtil.GetExtraActions(mySolutionTracker, myBackendUnityHost));
            }
        }
    }
}