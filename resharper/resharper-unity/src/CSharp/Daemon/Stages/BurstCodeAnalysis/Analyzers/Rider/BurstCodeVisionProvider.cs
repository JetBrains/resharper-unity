using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Host.Platform.Icons;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.ReSharper.Plugins.Unity.Rider.Highlightings.IconsProviders;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers.Rider
{
    [SolutionComponent]
    public sealed class BurstCodeVisionProvider : BurstProblemAnalyzerBase<IMethodDeclaration>
    {
        private readonly IApplicationWideContextBoundSettingStore mySettingsStore;
        private readonly BurstCodeInsightProvider myBurstCodeInsightProvider;
        private readonly IconHost myIconHost;
        private readonly BurstCodeInsights myCodeInsights;

        public BurstCodeVisionProvider(IApplicationWideContextBoundSettingStore store,
                                       BurstCodeInsightProvider burstCodeInsightProvider,
                                       IconHost iconHost,
                                       BurstCodeInsights codeInsights)
        {
            mySettingsStore = store;
            myBurstCodeInsightProvider = burstCodeInsightProvider;
            myIconHost = iconHost;
            myCodeInsights = codeInsights;
        }

        protected override void Analyze(IMethodDeclaration methodDeclaration,
            IHighlightingConsumer consumer, IReadOnlyCallGraphContext context)
        {
            var boundStore = mySettingsStore.BoundSettingsStore;
            var providerId = myBurstCodeInsightProvider.ProviderId;
            
            if (!RiderIconProviderUtil.IsCodeVisionEnabled(boundStore, providerId, () => { }, out _))
                return;

            var declaredElement = methodDeclaration.DeclaredElement;
            var iconModel = myIconHost.Transform(InsightUnityIcons.InsightUnity.Id);
            var actions = myCodeInsights.GetBurstActions(methodDeclaration, context);

            myBurstCodeInsightProvider.AddHighlighting(consumer, methodDeclaration, declaredElement,
                BurstCodeAnalysisUtil.BURST_DISPLAY_NAME,
                BurstCodeAnalysisUtil.BURST_TOOLTIP,
                BurstCodeAnalysisUtil.BURST_DISPLAY_NAME,
                iconModel,
                actions,
                extraActions: null);
        }

        protected override bool CheckAndAnalyze(IMethodDeclaration methodDeclaration, IHighlightingConsumer consumer,
            IReadOnlyCallGraphContext context)
        {
            return false;
        }
    }
}