using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Analyzers
{
    public abstract class PerformanceProblemAnalyzerBase<T> : UnityProblemAnalyzerBase<T> where T : ITreeNode
    {
        public override UnityProblemAnalyzerContextElement Context => UnityProblemAnalyzerContextElement.PERFORMANCE_CONTEXT;
        public override UnityProblemAnalyzerContextElement ProhibitedContext => UnityProblemAnalyzerContextElement.NONE;
    }
}