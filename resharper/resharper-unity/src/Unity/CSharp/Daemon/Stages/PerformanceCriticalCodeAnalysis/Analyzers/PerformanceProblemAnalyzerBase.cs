using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.CallGraphStage;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Analyzers
{
    public abstract class PerformanceProblemAnalyzerBase<T> : CallGraphProblemAnalyzerBase<T> where T : ITreeNode
    {
        private const CallGraphContextTag Context = CallGraphContextTag.PERFORMANCE_CRITICAL_CONTEXT;
        protected override bool IsApplicable(IReadOnlyCallGraphContext context)
        {
            return context.IsSuperSetOf(Context);
        }
    }
}