using JetBrains.Application.Parts;
using JetBrains.Application.Settings;
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
    [SolutionComponent(Instantiation.DemandAnyThreadSafe)]
    public sealed class BurstCodeVisionProvider(
        Lifetime lifetime,
        ISettingsStore settingsStore,
        IApplicationWideContextBoundSettingStore store,
        BurstCodeInsightProvider burstCodeInsightProvider,
        IconHost iconHost,
        BurstCodeInsights codeInsights)
        : BurstGutterMarkProvider(lifetime, settingsStore, codeInsights)
    {
        private readonly IApplicationWideContextBoundSettingStore mySettingsStore = store;
        private readonly BurstCodeInsights myCodeInsights = codeInsights;

        public override bool IsGutterMarkEnabled
        {
            get
            {
                var result = false;

                RiderIconProviderUtil.IsCodeVisionEnabled(
                    mySettingsStore.BoundSettingsStore,
                    burstCodeInsightProvider.ProviderId,
                    () => result = base.IsGutterMarkEnabled,
                    out _
                );

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
            var providerId = burstCodeInsightProvider.ProviderId;
            void Fallback() => base.Analyze(methodDeclaration, consumer, context);

            return isBurstIconsEnabled && !HasBurstAttributeInstance(methodDeclaration)
                                       && RiderIconProviderUtil.IsCodeVisionEnabled(boundStore, providerId, Fallback,
                                           out _);
        }

        protected override void Analyze(IMethodDeclaration methodDeclaration,
            IHighlightingConsumer consumer, IReadOnlyCallGraphContext context)
        {
            if (!ShouldAddCodeVision(methodDeclaration, consumer, context))
                return;

            var declaredElement = methodDeclaration.DeclaredElement;
            var iconModel = iconHost.Transform(InsightUnityIcons.InsightUnity.Id);
            var actions = myCodeInsights.GetBurstActions(methodDeclaration, context);

            burstCodeInsightProvider.AddHighlighting(consumer, methodDeclaration, declaredElement,
                BurstCodeAnalysisUtil.BurstDisplayName,
                BurstCodeAnalysisUtil.BurstTooltip,
                BurstCodeAnalysisUtil.BurstDisplayName,
                iconModel,
                actions,
                extraActions: null);
        }
    }
}