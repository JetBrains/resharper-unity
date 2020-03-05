using JetBrains.ReSharper.Daemon.CallGraph;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Analyzers
{
    public abstract class PerformanceProblemAnalyzerBase<T> : UnityProblemAnalyzerBase<T>
    {
        public override UnityProblemAnalyzerContext Context { get; } = UnityProblemAnalyzerContext.PERFOMANCE_CONTEXT;
    }
}