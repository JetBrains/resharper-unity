using JetBrains.Application.Settings;
using JetBrains.Application.Settings.Implementation;
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
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Plugins.Unity.Yaml;
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
        private readonly AssetSerializationMode myAssetSerializationMode;

        public RiderFieldDetector(ISolution solution, CallGraphSwaExtensionProvider callGraphSwaExtensionProvider, 
            SettingsStore settingsStore, PerformanceCriticalCodeCallGraphMarksProvider marksProvider, UnityApi unityApi,
            UnityCodeInsightFieldUsageProvider fieldUsageProvider,
            UnitySolutionTracker solutionTracker, ConnectionTracker connectionTracker,
            IconHost iconHost, AssetSerializationMode assetSerializationMode, IElementIdProvider provider)
            : base(solution, callGraphSwaExtensionProvider, settingsStore, marksProvider, unityApi, provider)
        {
            myFieldUsageProvider = fieldUsageProvider;
            mySolutionTracker = solutionTracker;
            myConnectionTracker = connectionTracker;
            myIconHost = iconHost;
            myAssetSerializationMode = assetSerializationMode;
        }

        protected override void AddMonoBehaviourHighlighting(IHighlightingConsumer consumer, ICSharpDeclaration element,
            string text,
            string tooltip, DaemonProcessKind kind)
        {
            if (!myAssetSerializationMode.IsForceText || 
                !Settings.GetValue((UnitySettings key) => key.EnableInspectorPropertiesEditor) ||
                !Settings.GetValue((UnitySettings key) => key.IsYamlParsingEnabled))
            {
                AddHighlighting(consumer, element, text, tooltip, kind);
                return;
            }
            
            if (RiderIconProviderUtil.IsCodeVisionEnabled(Settings, myFieldUsageProvider.ProviderId,
                () => { base.AddHighlighting(consumer, element, text, tooltip, kind); }, out var useFallback))
            {
                if (!useFallback)
                {
                    consumer.AddImplicitConfigurableHighlighting(element);
                }
                myFieldUsageProvider.AddInspectorHighlighting(consumer, element, element.DeclaredElement, text,
                    tooltip, text, myIconHost.Transform(InsightUnityIcons.InsightUnity.Id), GetActions(element),
                    RiderIconProviderUtil.GetExtraActions(mySolutionTracker, myConnectionTracker));
            }
        }


        protected override void AddHighlighting(IHighlightingConsumer consumer, ICSharpDeclaration element, string text,
            string tooltip,
            DaemonProcessKind kind)
        {
            if (RiderIconProviderUtil.IsCodeVisionEnabled(Settings, myFieldUsageProvider.ProviderId,
                () => { base.AddHighlighting(consumer, element, text, tooltip, kind); }, out var useFallback))
            {
                if (!useFallback)
                {
                    consumer.AddImplicitConfigurableHighlighting(element);
                }
                myFieldUsageProvider.AddHighlighting(consumer, element, element.DeclaredElement, text,
                    tooltip, text, myIconHost.Transform(InsightUnityIcons.InsightUnity.Id), GetActions(element),
                    RiderIconProviderUtil.GetExtraActions(mySolutionTracker, myConnectionTracker));
            }
        }
    }
}