using JetBrains.Application.Settings.Implementation;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Host.Platform.Icons;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Analyzers;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.ReSharper.Plugins.Unity.Rider.CodeInsights;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Highlightings.IconsProviders
{
    [SolutionComponent]
    public class RiderUnityCommonIconProvider : UnityCommonIconProvider
    {
        private readonly UnityCodeInsightProvider myCodeInsightProvider;
        private readonly UnitySolutionTracker mySolutionTracker;
        private readonly ConnectionTracker myConnectionTracker;
        private readonly IconHost myIconHost;

        public RiderUnityCommonIconProvider(ISolution solution, SolutionAnalysisService swa, SettingsStore settingsStore,
            PerformanceCriticalCodeCallGraphAnalyzer analyzer, UnityApi api,
            UnityCodeInsightProvider codeInsightProvider,
            UnitySolutionTracker solutionTracker, ConnectionTracker connectionTracker,
            IconHost iconHost)
            : base(solution, swa, settingsStore, analyzer, api)
        {
            myCodeInsightProvider = codeInsightProvider;
            mySolutionTracker = solutionTracker;
            myConnectionTracker = connectionTracker;
            myIconHost = iconHost;
        }

        public override void AddEventFunctionHighlighting(IHighlightingConsumer consumer, IMethod method, UnityEventFunction eventFunction,
            string text, string tooltip, DaemonProcessKind kind)
        {
            var iconId = method.HasHotIcon(Swa, Settings, Analyzer, kind)
                ? InsightUnityIcons.InsightHot.Id
                : InsightUnityIcons.InsightUnity.Id;
            
            if (RiderIconProviderUtil.IsCodeVisionEnabled(Settings, myCodeInsightProvider.ProviderId,
                () => { base.AddEventFunctionHighlighting(consumer, method, eventFunction, text, tooltip, kind);}))
            {
                foreach (var declaration in method.GetDeclarations())
                {
                    if (declaration is ICSharpDeclaration cSharpDeclaration)
                    {
                        consumer.AddImplicitConfigurableHighlighting(cSharpDeclaration);
                        myCodeInsightProvider.AddHighlighting(consumer, cSharpDeclaration, method, text, tooltip, text,
                            myIconHost.Transform(iconId), GetEventFunctionActions(cSharpDeclaration), RiderIconProviderUtil.GetExtraActions(mySolutionTracker, myConnectionTracker));
                    }
                }
            }
        }

        public override void AddFrequentlyCalledMethodHighlighting(IHighlightingConsumer consumer, ICSharpDeclaration declaration, string text,
            string tooltip, DaemonProcessKind kind)
        {
            var isHot = declaration.HasHotIcon(Swa, Settings, Analyzer, kind);
            if (!isHot)
                return;
            
            if (RiderIconProviderUtil.IsCodeVisionEnabled(Settings, myCodeInsightProvider.ProviderId,
                () => { base.AddFrequentlyCalledMethodHighlighting(consumer, declaration, text, tooltip, kind);}))
            {
                myCodeInsightProvider.AddHighlighting(consumer, declaration, declaration.DeclaredElement, text, tooltip, text,
                    myIconHost.Transform(InsightUnityIcons.InsightHot.Id), EmptyList<BulbMenuItem>.Instance, RiderIconProviderUtil.GetExtraActions(mySolutionTracker, myConnectionTracker));
            }
        }
    }
}