using System.Collections.Generic;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.CallGraphStage
{
    [DaemonStage(StagesBefore = new[] {typeof(CSharpErrorStage)})]
    public class CallGraphLocalStage : CallGraphAbstractStage
    {
        public CallGraphLocalStage(
            IEnumerable<ICallGraphContextProvider> contextProviders, 
            IEnumerable<ICallGraphProblemAnalyzer> problemAnalyzers, 
            ILogger logger)
            : base(contextProviders, problemAnalyzers, logger)
        {
        }
    }
}