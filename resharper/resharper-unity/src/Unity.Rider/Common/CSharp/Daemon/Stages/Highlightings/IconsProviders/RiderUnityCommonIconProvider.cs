using System.Collections.Generic;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.ReSharper.Plugins.Unity.Rider.Common.CSharp.Daemon.CodeInsights;
using JetBrains.ReSharper.Plugins.Unity.Rider.Common.Protocol;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Rider.Backend.Platform.Icons;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Common.CSharp.Daemon.Stages.Highlightings.IconsProviders
{
    [SolutionComponent]
    public sealed class RiderUnityCommonIconProvider : UnityCommonIconProvider
    {
        private readonly UnityCodeInsightProvider myCodeInsightProvider;
        private readonly UnitySolutionTracker mySolutionTracker;
        private readonly IBackendUnityHost myBackendUnityHost;
        private readonly IconHost myIconHost;

        public RiderUnityCommonIconProvider(ISolution solution,
                                            IApplicationWideContextBoundSettingStore settingsStore,
                                            UnityApi api,
                                            UnityCodeInsightProvider codeInsightProvider,
                                            UnitySolutionTracker solutionTracker,
                                            IBackendUnityHost backendUnityHost,
                                            IconHost iconHost, PerformanceCriticalContextProvider contextProvider,
                                            IEnumerable<IPerformanceAnalysisBulbItemsProvider> menuItemProviders)
            : base(solution, api, settingsStore, contextProvider, menuItemProviders)
        {
            myCodeInsightProvider = codeInsightProvider;
            mySolutionTracker = solutionTracker;
            myBackendUnityHost = backendUnityHost;
            myIconHost = iconHost;
        }

        public override void AddEventFunctionHighlighting(IHighlightingConsumer consumer,
                                                          IMethod method,
                                                          UnityEventFunction eventFunction,
                                                          string text,
                                                          IReadOnlyCallGraphContext context)
        {
            var boundStore = SettingsStore.BoundSettingsStore;
            var providerId = myCodeInsightProvider.ProviderId;
            void Fallback() => base.AddEventFunctionHighlighting(consumer, method, eventFunction, text, context);

            // here is order of IsCodeVisionEnabled and hasHotIcon matters
            // hasHotIcon differs if hot icon or event function icon, it depends on multiple settings
            if (!RiderIconProviderUtil.IsCodeVisionEnabled(boundStore, providerId, Fallback, out var useFallback))
                return;

            var iconId = method.HasHotIcon(PerformanceContextProvider, boundStore, context)
                ? InsightUnityIcons.InsightHot.Id
                : InsightUnityIcons.InsightUnity.Id;
            var iconModel = myIconHost.Transform(iconId);
            var extraActions = RiderIconProviderUtil.GetExtraActions(mySolutionTracker, myBackendUnityHost);

            foreach (var declaration in method.GetDeclarations())
            {
                if (!(declaration is ICSharpDeclaration cSharpDeclaration))
                    continue;

                if (!useFallback)
                    consumer.AddImplicitConfigurableHighlighting(cSharpDeclaration);

                var actions = GetEventFunctionActions(cSharpDeclaration, context);

                myCodeInsightProvider.AddHighlighting(consumer, cSharpDeclaration, method, text,
                    eventFunction.Description ?? string.Empty, text, iconModel, actions, extraActions);
            }
        }

        public override void AddFrequentlyCalledMethodHighlighting(IHighlightingConsumer consumer,
            ICSharpDeclaration cSharpDeclaration, string text, string tooltip, IReadOnlyCallGraphContext context)
        {
            var boundStore = SettingsStore.BoundSettingsStore;

            // here is order of IsCodeVisionEnabled and hasHotIcon matters also
            // hasHotIcon and IsCodeVisionEnabled checks if hot icon should be enabled. if it shouldn't - nothing would be displayed
            if (!cSharpDeclaration.HasHotIcon(PerformanceContextProvider, boundStore, context))
                return;

            void Fallback() => base.AddFrequentlyCalledMethodHighlighting(consumer, cSharpDeclaration, text, tooltip, context);
            var providerId = myCodeInsightProvider.ProviderId;

            if (!RiderIconProviderUtil.IsCodeVisionEnabled(boundStore, providerId, Fallback, out _))
                return;

            // code vision
            var actions = GetActions(cSharpDeclaration, context);
            var extraActions = RiderIconProviderUtil.GetExtraActions(mySolutionTracker, myBackendUnityHost);
            var iconModel = myIconHost.Transform(InsightUnityIcons.InsightHot.Id);

            myCodeInsightProvider.AddHighlighting(consumer, cSharpDeclaration, cSharpDeclaration.DeclaredElement,
                text, tooltip, text, iconModel, actions, extraActions);
        }
    }
}