namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Analyzers
{
    public abstract class PerformanceProblemAnalyzerBase<T> : UnityProblemAnalyzerBase<T>
    {
        public override UnityProblemAnalyzerContext Context { get; } = UnityProblemAnalyzerContext.PERFORMANCE_CONTEXT;
    }
}