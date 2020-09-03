using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Analyzers
{
    public abstract class PerformanceProblemAnalyzerBase<T> : UnityProblemAnalyzerBase<T>
    {
        public override UnityProblemAnalyzerContextElement Context => UnityProblemAnalyzerContextElement.PERFORMANCE_CONTEXT;
        public override UnityProblemAnalyzerContextElement ProhibitedContext => UnityProblemAnalyzerContextElement.NONE;
    }
}