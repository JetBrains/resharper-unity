using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.CommonCodeAnalysis.Analyzers
{
    public abstract class CommonProblemAnalyzerBase<T> : UnityProblemAnalyzerBase<T> where T : ITreeNode
    {
        public override UnityProblemAnalyzerContextElement Context => UnityProblemAnalyzerContextElement.NONE;
        public override UnityProblemAnalyzerContextElement ProhibitedContext => UnityProblemAnalyzerContextElement.NONE;
    }
}