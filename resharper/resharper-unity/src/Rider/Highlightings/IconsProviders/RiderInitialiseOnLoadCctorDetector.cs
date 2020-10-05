using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Host.Platform.Icons;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.ReSharper.Plugins.Unity.Rider.CodeInsights;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Highlightings.IconsProviders
{
    [SolutionComponent]
    public class RiderInitialiseOnLoadCctorDetector : InitialiseOnLoadCctorDetector
    {
        private readonly UnityCodeInsightFieldUsageProvider myFieldUsageProvider;
        private readonly UnitySolutionTracker mySolutionTracker;
        private readonly ConnectionTracker myConnectionTracker;
        private readonly IconHost myIconHost;

        public RiderInitialiseOnLoadCctorDetector(ISolution solution,
                                                  CallGraphSwaExtensionProvider callGraphSwaExtensionProvider,
                                                  IApplicationWideContextBoundSettingStore settingsStore,
                                                  PerformanceCriticalCodeCallGraphMarksProvider marksProvider,
                                                  UnityCodeInsightFieldUsageProvider fieldUsageProvider,
                                                  UnitySolutionTracker solutionTracker,
                                                  ConnectionTracker connectionTracker,
                                                  IconHost iconHost, IElementIdProvider elementIdProvider)
            : base(solution, callGraphSwaExtensionProvider, settingsStore, marksProvider, elementIdProvider)
        {
            myFieldUsageProvider = fieldUsageProvider;
            mySolutionTracker = solutionTracker;
            myConnectionTracker = connectionTracker;
            myIconHost = iconHost;
        }

        protected override void AddHighlighting(IHighlightingConsumer consumer, ICSharpDeclaration element, string text, string tooltip,
                                                DaemonProcessKind kind)
        {
            var iconId = element.HasHotIcon(CallGraphSwaExtensionProvider, SettingsStore.BoundSettingsStore, MarksProvider, kind, ElementIdProvider)
                ? InsightUnityIcons.InsightHot.Id
                : InsightUnityIcons.InsightUnity.Id;

            if (RiderIconProviderUtil.IsCodeVisionEnabled(SettingsStore.BoundSettingsStore, myFieldUsageProvider.ProviderId,
                () => { base.AddHighlighting(consumer, element, text, tooltip, kind); }, out var useFallback))
            {
                if (!useFallback)
                {
                    consumer.AddImplicitConfigurableHighlighting(element);
                }
                myFieldUsageProvider.AddHighlighting(consumer, element, element.DeclaredElement, text,
                    tooltip, text, myIconHost.Transform(iconId), GetActions(element),
                    RiderIconProviderUtil.GetExtraActions(mySolutionTracker, myConnectionTracker));
            }
        }
    }
}