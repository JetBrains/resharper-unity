using System.Collections.Generic;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Feature.Services.Daemon;
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
            PerformanceCriticalCodeCallGraphAnalyzer performanceCriticalCodeCallGraphAnalyzer,
            SolutionAnalysisService swa, IEnumerable<IUnityDeclarationHighlightingProvider> higlightingProviders,
            IEnumerable<IPerformanceProblemAnalyzer> performanceProblemAnalyzers,
            UnityApi api, UnityCommonIconProvider commonIconProvider, ILogger logger)
            : base(callGraphSwaExtensionProvider, performanceCriticalCodeCallGraphAnalyzer, swa, higlightingProviders,
                performanceProblemAnalyzers, api, commonIconProvider, logger)
        {
        }
    }
}