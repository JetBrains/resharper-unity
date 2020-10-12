using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.CallGraphStage;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers
{
    public abstract class BurstProblemAnalyzerBase<T> : CallGraphProblemAnalyzerBase<T>, IBurstBannedAnalyzer where T : ITreeNode
    {
        public override CallGraphContextElement Context => CallGraphContextElement.BURST_CONTEXT;
        public override CallGraphContextElement ProhibitedContext => CallGraphContextElement.NONE;

        protected override void Analyze(T t, IDaemonProcess daemonProcess, DaemonProcessKind kind, IHighlightingConsumer consumer)
        {
            CheckAndAnalyze(t, consumer);
        }

        protected abstract bool CheckAndAnalyze(T t, [CanBeNull] IHighlightingConsumer consumer);
        public bool Check(ITreeNode node)
        {
            if (node is T t)
                return CheckAndAnalyze(t, null);
            return false;
        }
    }
}