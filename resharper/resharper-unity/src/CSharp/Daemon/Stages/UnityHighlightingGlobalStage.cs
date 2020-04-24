using System.Collections.Generic;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Analyzers;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.CallGraph;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages
{
    [DaemonStage(GlobalAnalysisStage = true, OverridenStages = new[] {typeof(UnityHighlightingStage)})]
    public class UnityHighlightingGlobalStage : UnityHighlightingAbstractStage
    {
        public UnityHighlightingGlobalStage(CallGraphSwaExtensionProvider callGraphSwaExtensionProvider,
            PerformanceCriticalCodeCallGraphMarksProvider performanceCriticalCodeCallGraphMarksProvider,
            CallGraphBurstMarksProvider callGraphBurstMarksProvider,
            IEnumerable<IUnityDeclarationHighlightingProvider> higlightingProviders,
            IEnumerable<IUnityProblemAnalyzer> performanceProblemAnalyzers,
            UnityApi api, UnityCommonIconProvider commonIconProvider, IElementIdProvider provider, ILogger logger)
            : base(callGraphSwaExtensionProvider, performanceCriticalCodeCallGraphMarksProvider, callGraphBurstMarksProvider,
                higlightingProviders, performanceProblemAnalyzers, api, commonIconProvider, provider, logger)
        {
        }
    }
}