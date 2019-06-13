using JetBrains.Application.Settings.Implementation;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Host.Platform.Icons;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Analyzers;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.ReSharper.Plugins.Unity.Rider.CodeInsights;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Highlightings.IconsProviders
{
    [SolutionComponent]
    public class RiderFieldDetector : FieldDetector
    {
        private readonly UnityCodeInsightFieldUsageProvider myFieldUsageProvider;
        private readonly UnitySolutionTracker mySolutionTracker;
        private readonly ConnectionTracker myConnectionTracker;
        private readonly IconHost myIconHost;

        public RiderFieldDetector(ISolution solution, SolutionAnalysisService swa, SettingsStore settingsStore,
            PerformanceCriticalCodeCallGraphAnalyzer analyzer, UnityApi unityApi,
            UnityCodeInsightFieldUsageProvider fieldUsageProvider,
            UnitySolutionTracker solutionTracker, ConnectionTracker connectionTracker,
            IconHost iconHost)
            : base(solution, swa, settingsStore, analyzer, unityApi)
        {
            myFieldUsageProvider = fieldUsageProvider;
            mySolutionTracker = solutionTracker;
            myConnectionTracker = connectionTracker;
            myIconHost = iconHost;
        }

        protected override void AddMonoBehaviourHighlighting(IHighlightingConsumer consumer, ICSharpDeclaration element, string text,
            string tooltip, DaemonProcessKind kind)
        {
            myFieldUsageProvider.AddInspectorHighlighting(consumer, element, element.DeclaredElement, myIconHost.Transform(InsightUnityIcons.InsightUnity.Id));
        }


        protected override void AddHighlighting(IHighlightingConsumer consumer, ICSharpDeclaration element, string text,
            string tooltip,
            DaemonProcessKind kind)
        {
            if (RiderIconProviderUtil.IsCodeVisionEnabled(Settings, myFieldUsageProvider.ProviderId,
                () => { base.AddHighlighting(consumer, element, text, tooltip, kind); }))
            {
                myFieldUsageProvider.AddHighlighting(consumer, element, element.DeclaredElement, text,
                    tooltip, text, myIconHost.Transform(InsightUnityIcons.InsightUnity.Id), GetActions(element),
                    RiderIconProviderUtil.GetExtraActions(mySolutionTracker, myConnectionTracker));
            }
        }
    }
}