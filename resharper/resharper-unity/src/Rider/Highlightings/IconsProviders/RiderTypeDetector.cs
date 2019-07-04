using JetBrains.Application.Settings;
using JetBrains.Application.Settings.Implementation;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Host.Platform.Icons;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Analyzers;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.ReSharper.Plugins.Unity.Rider.CodeInsights;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Highlightings.IconsProviders
{
    [SolutionComponent]
    public class RiderTypeDetector : TypeDetector
    {
        private readonly UnityCodeInsightProvider myCodeInsightProvider;
        private readonly UnitySolutionTracker mySolutionTracker;
        private readonly ConnectionTracker myConnectionTracker;
        private readonly IconHost myIconHost;

        public RiderTypeDetector(ISolution solution, SolutionAnalysisService swa, CallGraphSwaExtensionProvider callGraphSwaExtensionProvider, 
            SettingsStore settingsStore, PerformanceCriticalCodeCallGraphAnalyzer analyzer, UnityApi unityApi,
            UnityCodeInsightProvider codeInsightProvider,
            UnitySolutionTracker solutionTracker, ConnectionTracker connectionTracker,
            IconHost iconHost)
            : base(solution, swa, callGraphSwaExtensionProvider, settingsStore, unityApi, analyzer)
        {
            myCodeInsightProvider = codeInsightProvider;
            mySolutionTracker = solutionTracker;
            myConnectionTracker = connectionTracker;
            myIconHost = iconHost;
        }

        protected override void AddMonoBehaviourHiglighting(IHighlightingConsumer consumer, IClassLikeDeclaration declaration, string text,
            string tooltip, DaemonProcessKind kind)
        {
            if (Settings.GetValue((UnitySettings key) => key.GutterIconMode) == GutterIconMode.Always)
            {
                base.AddHighlighting(consumer, declaration, text, tooltip, kind);
            }
        }

        protected override void AddHighlighting(IHighlightingConsumer consumer, ICSharpDeclaration element, string text,
            string tooltip,
            DaemonProcessKind kind)
        {
            if (RiderIconProviderUtil.IsCodeVisionEnabled(Settings, myCodeInsightProvider.ProviderId,
                () => { base.AddHighlighting(consumer, element, text, tooltip, kind); }, out var useFallback))
            {
                if (!useFallback)
                {
                    consumer.AddImplicitConfigurableHighlighting(element);
                }
                myCodeInsightProvider.AddHighlighting(consumer, element, element.DeclaredElement, text,
                    tooltip, text, myIconHost.Transform(InsightUnityIcons.InsightUnity.Id), GetActions(element),
                    RiderIconProviderUtil.GetExtraActions(mySolutionTracker, myConnectionTracker));
            }
        }
    }
}