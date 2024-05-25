using System.Collections.Generic;
using JetBrains.Application.Parts;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.CallGraphStage
{
    [DaemonStage(Instantiation.DemandAnyThreadUnsafe, GlobalAnalysisStage = true,
        StagesBefore = new[] {typeof(SolutionAnalysisFileStructureCollectorStage)},
        OverridenStages = new[] {typeof(CallGraphLocalStage)})]
    public class CallGraphGlobalStage : CallGraphAbstractStage
    {
        public CallGraphGlobalStage(CallGraphSwaExtensionProvider swaExtensionProvider, IEnumerable<ICallGraphContextProvider> contextProviders, IEnumerable<ICallGraphProblemAnalyzer> problemAnalyzers, ILogger logger)
            : base(swaExtensionProvider, contextProviders, problemAnalyzers, logger)
        {
        }
    }
}