using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.ReSharper.Plugins.Unity.Rider.Common.CSharp.Daemon.Stages.Highlightings.IconsProviders;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Rider.Backend.Platform.Icons;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Common.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers
{
    [SolutionComponent]
    public sealed class BurstCodeVisionProvider : BurstGutterMarkProvider
    {
        private readonly IApplicationWideContextBoundSettingStore mySettingsStore;
        private readonly BurstCodeInsightProvider myBurstCodeInsightProvider;
        private readonly IconHost myIconHost;
        private readonly BurstCodeInsights myCodeInsights;

        public BurstCodeVisionProvider(
            Lifetime lifetime,
            IApplicationWideContextBoundSettingStore store,
            BurstCodeInsightProvider burstCodeInsightProvider,
            IconHost iconHost,
            BurstCodeInsights codeInsights) : base(lifetime, store, codeInsights)
        {
            mySettingsStore = store;
            myBurstCodeInsightProvider = burstCodeInsightProvider;
            myIconHost = iconHost;
            myCodeInsights = codeInsights;
        }

        public override bool IsGutterMarkEnabled
        {
            get
            {
                var result = false;
                var boundStore = mySettingsStore.BoundSettingsStore;
                var providerId = myBurstCodeInsightProvider.ProviderId;

                RiderIconProviderUtil.IsCodeVisionEnabled(boundStore, providerId, () => result = base.IsGutterMarkEnabled, out _);

                return result;
            }
        }

        private bool ShouldAddCodeVision(
            IMethodDeclaration methodDeclaration,
            IHighlightingConsumer consumer,
            IReadOnlyCallGraphContext context)
        {
            var isBurstIconsEnabled = base.IsGutterMarkEnabled;
            var boundStore = mySettingsStore.BoundSettingsStore;
            var providerId = myBurstCodeInsightProvider.ProviderId;
            void Fallback() => base.Analyze(methodDeclaration, consumer, context);

            return isBurstIconsEnabled
                   && RiderIconProviderUtil.IsCodeVisionEnabled(boundStore, providerId, Fallback, out _);
        }

        protected override void Analyze(IMethodDeclaration methodDeclaration,
            IHighlightingConsumer consumer, IReadOnlyCallGraphContext context)
        {
            if (!ShouldAddCodeVision(methodDeclaration, consumer, context))
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
    }
}
