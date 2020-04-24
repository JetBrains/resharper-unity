using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Analyzers;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers
{
    public abstract class BurstProblemAnalyzerBase<T> : UnityProblemAnalyzerBase<T>
    {
        public override UnityProblemAnalyzerContext Context { get; } = UnityProblemAnalyzerContext.BURST_CONTEXT;
    }
}