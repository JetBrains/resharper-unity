using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.CallGraphStage
{
    public abstract class CallGraphProblemAnalyzerBase<T> : ICallGraphProblemAnalyzer where T : ITreeNode
    {
        public void RunInspection(ITreeNode node, IDaemonProcess daemonProcess, DaemonProcessKind kind,
            IHighlightingConsumer consumer, IReadOnlyContext context)
        {
            if (node is T t && IsApplicable(context))
                Analyze(t, daemonProcess, kind, consumer, context);
        }

        protected abstract bool IsApplicable(IReadOnlyContext context);

        protected abstract void Analyze([NotNull] T t, IDaemonProcess daemonProcess, DaemonProcessKind kind,
            [NotNull] IHighlightingConsumer consumer, IReadOnlyContext context);
    }
}