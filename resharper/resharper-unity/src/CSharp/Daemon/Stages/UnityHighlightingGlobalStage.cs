using System;
using System.Collections.Generic;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Analyzers;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.CallGraph;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages
{
    [DaemonStage(GlobalAnalysisStage = true, OverridenStages = new Type[] {typeof(UnityHighlightingStage)})]
    public class UnityHighlightingGlobalStage : UnityHighlightingAbstractStage
    {
        public UnityHighlightingGlobalStage(PerformanceCriticalCodeMarksProvider performanceCriticalCodeMarksProvider,
            BurstMarksProvider burstMarksProvider,
            IEnumerable<IUnityDeclarationHighlightingProvider> highlightingProviders,
            IEnumerable<IUnityProblemAnalyzer> problemAnalyzers,
            UnityApi api, UnityCommonIconProvider commonIconProvider, ILogger logger, UnityProblemAnalyzerContextSystem contextSystem)
            : base(highlightingProviders, problemAnalyzers, api, commonIconProvider, logger, contextSystem)
        {
        }
    }
}