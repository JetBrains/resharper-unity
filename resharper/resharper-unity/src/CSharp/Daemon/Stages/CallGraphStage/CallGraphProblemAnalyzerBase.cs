using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.CallGraphStage
{
    public abstract class CallGraphProblemAnalyzerBase<T> : ICallGraphProblemAnalyzer where T : ITreeNode
    {
        public abstract CallGraphContextElement Context { get; }
        public abstract CallGraphContextElement ProhibitedContext { get; }

        public void RunInspection(ITreeNode node, IDaemonProcess daemonProcess, DaemonProcessKind kind,
            IHighlightingConsumer consumer, CallGraphContext context)
        {
            if (context.ContainAny(ProhibitedContext))
                return;
            
            if (node is T t)
                Analyze(t, daemonProcess, kind, consumer);
        }


        protected abstract void Analyze(T t, IDaemonProcess daemonProcess, DaemonProcessKind kind,
            IHighlightingConsumer consumer);
    }
}