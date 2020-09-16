using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Analyzers
{
    public abstract class PerformanceProblemAnalyzerBase<T> : UnityProblemAnalyzerBase<T> where T : ITreeNode
    {
        public override UnityProblemAnalyzerContext Context { get; } = UnityProblemAnalyzerContext.PERFORMANCE_CONTEXT;
    }
}