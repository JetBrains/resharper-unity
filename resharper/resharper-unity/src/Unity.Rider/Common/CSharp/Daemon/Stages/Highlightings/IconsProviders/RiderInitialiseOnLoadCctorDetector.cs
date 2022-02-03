using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.ReSharper.Plugins.Unity.Rider.Common.CSharp.Daemon.CodeInsights;
using JetBrains.ReSharper.Plugins.Unity.Rider.Common.Protocol;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Rider.Backend.Platform.Icons;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Common.CSharp.Daemon.Stages.Highlightings.IconsProviders
{
    [SolutionComponent]
    public class RiderInitialiseOnLoadCctorDetector : InitialiseOnLoadCctorDetector
    {
        private readonly UnityCodeInsightFieldUsageProvider myFieldUsageProvider;
        private readonly UnitySolutionTracker mySolutionTracker;
        private readonly IBackendUnityHost myBackendUnityHost;
        private readonly IconHost myIconHost;

        public RiderInitialiseOnLoadCctorDetector(ISolution solution,
                                                  IApplicationWideContextBoundSettingStore settingsStore,
                                                  UnityCodeInsightFieldUsageProvider fieldUsageProvider,
                                                  UnitySolutionTracker solutionTracker,
                                                  IBackendUnityHost backendUnityHost,
                                                  IconHost iconHost, PerformanceCriticalContextProvider contextProvider)
            : base(solution, settingsStore, contextProvider)
        {
            myFieldUsageProvider = fieldUsageProvider;
            mySolutionTracker = solutionTracker;
            myBackendUnityHost = backendUnityHost;
            myIconHost = iconHost;
        }

        protected override void AddHighlighting(IHighlightingConsumer consumer, ICSharpDeclaration element, string text, string tooltip,
                                                IReadOnlyCallGraphContext context)
        {
            var iconId = element.HasHotIcon(ContextProvider, SettingsStore.BoundSettingsStore, context)
                ? InsightUnityIcons.InsightHot.Id
                : InsightUnityIcons.InsightUnity.Id;

            if (RiderIconProviderUtil.IsCodeVisionEnabled(SettingsStore.BoundSettingsStore, myFieldUsageProvider.ProviderId,
                () => { base.AddHighlighting(consumer, element, text, tooltip, context); }, out var useFallback))
            {
                if (!useFallback)
                {
                    consumer.AddImplicitConfigurableHighlighting(element);
                }
                myFieldUsageProvider.AddHighlighting(consumer, element, element.DeclaredElement, text,
                    tooltip, text, myIconHost.Transform(iconId), GetActions(element),
                    RiderIconProviderUtil.GetExtraActions(mySolutionTracker, myBackendUnityHost));
            }
        }
    }
}