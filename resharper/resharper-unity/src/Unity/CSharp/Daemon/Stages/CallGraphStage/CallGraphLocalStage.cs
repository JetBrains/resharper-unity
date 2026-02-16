using JetBrains.Application.Components;
using JetBrains.Application.Parts;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.CallGraphStage
{
    [DaemonStage(Instantiation.DemandAnyThreadSafe, StagesBefore = [typeof(CSharpErrorStage)])]
    public class CallGraphLocalStage : CallGraphAbstractStage
    {
        public CallGraphLocalStage(
            ILazy<CallGraphSwaExtensionProvider> swaExtensionProvider,
            IImmutableEnumerable<ICallGraphContextProvider> contextProviders, 
            IImmutableEnumerable<ICallGraphProblemAnalyzer> problemAnalyzers, 
            ILogger logger)
            : base(swaExtensionProvider, contextProviders, problemAnalyzers, logger)
        {
        }
    }
}