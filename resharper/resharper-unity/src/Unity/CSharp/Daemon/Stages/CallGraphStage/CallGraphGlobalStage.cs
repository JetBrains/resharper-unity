using JetBrains.Application.Components;
using JetBrains.Application.Parts;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.CallGraphStage
{
    [DaemonStage(Instantiation.DemandAnyThreadSafe,
        GlobalAnalysisStage = true,
        StagesBefore = [typeof(SolutionAnalysisFileStructureCollectorStage)],
        OverridenStages = [typeof(CallGraphLocalStage)])]
    public class CallGraphGlobalStage : CallGraphAbstractStage
    {
        public CallGraphGlobalStage(ILazy<CallGraphSwaExtensionProvider> swaExtensionProvider, IImmutableEnumerable<ICallGraphContextProvider> contextProviders, IImmutableEnumerable<ICallGraphProblemAnalyzer> problemAnalyzers, ILogger logger)
            : base(swaExtensionProvider, contextProviders, problemAnalyzers, logger)
        {
        }
    }
}