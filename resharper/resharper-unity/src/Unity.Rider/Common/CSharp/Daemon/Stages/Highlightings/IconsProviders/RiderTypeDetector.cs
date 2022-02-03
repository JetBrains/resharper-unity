using System.Collections.Generic;
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
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Plugins.Unity.Yaml;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Rider.Backend.Platform.Icons;
using JetBrains.Rider.Model;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Common.CSharp.Daemon.Stages.Highlightings.IconsProviders
{
    [SolutionComponent]
    public class RiderTypeDetector : TypeDetector
    {
        private readonly AssetIndexingSupport myAssetIndexingSupport;
        private readonly UnityUsagesCodeVisionProvider myUsagesCodeVisionProvider;
        private readonly UnityCodeInsightProvider myCodeInsightProvider;
        [NotNull, ItemNotNull]
        private readonly IEnumerable<IScriptUsagesElementContainer> myScriptsUsagesElementContainers;
        private readonly DeferredCacheController myDeferredCacheController;
        private readonly UnitySolutionTracker mySolutionTracker;
        private readonly IBackendUnityHost myBackendUnityHost;
        private readonly IconHost myIconHost;
        private readonly AssetSerializationMode myAssetSerializationMode;

        public RiderTypeDetector(ISolution solution,
                                 IApplicationWideContextBoundSettingStore settingsStore,
                                 UnityApi unityApi,
                                 AssetIndexingSupport assetIndexingSupport,
                                 UnityUsagesCodeVisionProvider usagesCodeVisionProvider,
                                 UnityCodeInsightProvider codeInsightProvider,
                                 [NotNull, ItemNotNull] IEnumerable<IScriptUsagesElementContainer> scriptsUsagesElementContainers,
                                 DeferredCacheController deferredCacheController, UnitySolutionTracker solutionTracker,
                                 IBackendUnityHost backendUnityHost,
                                 IconHost iconHost, AssetSerializationMode assetSerializationMode,
                                 PerformanceCriticalContextProvider contextProvider)
            : base(solution, settingsStore, unityApi, contextProvider)
        {
            myAssetIndexingSupport = assetIndexingSupport;
            myUsagesCodeVisionProvider = usagesCodeVisionProvider;
            myCodeInsightProvider = codeInsightProvider;
            myScriptsUsagesElementContainers = scriptsUsagesElementContainers;
            myDeferredCacheController = deferredCacheController;
            mySolutionTracker = solutionTracker;
            myBackendUnityHost = backendUnityHost;
            myIconHost = iconHost;
            myAssetSerializationMode = assetSerializationMode;
        }

        protected override void AddMonoBehaviourHighlighting(IHighlightingConsumer consumer, IClassLikeDeclaration declaration, string text,
                                                             string tooltip, IReadOnlyCallGraphContext context)
        {
            if (RiderIconProviderUtil.IsCodeVisionEnabled(SettingsStore.BoundSettingsStore, myCodeInsightProvider.ProviderId,
                () => { base.AddHighlighting(consumer, declaration, text, tooltip, context); }, out var useFallback))
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

                if (!myAssetIndexingSupport.IsEnabled.Value || !myDeferredCacheController.CompletedOnce.Value || !myAssetSerializationMode.IsForceText)
                {
                    myCodeInsightProvider.AddHighlighting(consumer, declaration, declaration.DeclaredElement, text,
                        tooltip, text, iconModel, GetActions(declaration),
                        RiderIconProviderUtil.GetExtraActions(mySolutionTracker, myBackendUnityHost));
                }
                else
                {
                    AddScriptUsagesHighlighting(consumer, declaration, iconModel);
                }
            }
        }

        private void AddScriptUsagesHighlighting([NotNull] IHighlightingConsumer consumer,
                                                 [NotNull] IClassLikeDeclaration declaration,
                                                 [NotNull] IconModel iconModel)
        {
            var count = 0;
            var estimatedResult = false;
            foreach (var scriptUsagesContainer in myScriptsUsagesElementContainers)
            {
                count += scriptUsagesContainer.GetScriptUsagesCount(declaration, out var result);
                estimatedResult = estimatedResult || result;
            }
            myUsagesCodeVisionProvider.AddHighlighting(consumer, declaration, declaration.DeclaredElement, count,
                "Click to view usages in assets", "Assets usages", estimatedResult, iconModel);
        }

        protected override void AddHighlighting(IHighlightingConsumer consumer, ICSharpDeclaration element, string text,
            string tooltip,
            IReadOnlyCallGraphContext context)
        {
            if (RiderIconProviderUtil.IsCodeVisionEnabled(SettingsStore.BoundSettingsStore, myCodeInsightProvider.ProviderId,
                () => { base.AddHighlighting(consumer, element, text, tooltip, context); }, out var useFallback))
            {
                if (!useFallback)
                {
                    consumer.AddImplicitConfigurableHighlighting(element);
                }
                myCodeInsightProvider.AddHighlighting(consumer, element, element.DeclaredElement, text,
                    tooltip, text, myIconHost.Transform(InsightUnityIcons.InsightUnity.Id), GetActions(element),
                    RiderIconProviderUtil.GetExtraActions(mySolutionTracker, myBackendUnityHost));
            }
        }
    }
}