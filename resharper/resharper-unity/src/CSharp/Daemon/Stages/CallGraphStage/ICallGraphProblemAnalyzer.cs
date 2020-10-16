using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.CallGraphStage
{
    public interface ICallGraphProblemAnalyzer
    {
        CallGraphContextElement Context { get; }
        CallGraphContextElement ProhibitedContext { get; }
        void RunInspection([NotNull] ITreeNode node, IDaemonProcess daemonProcess, DaemonProcessKind kind, [NotNull] IHighlightingConsumer consumer, [NotNull] CallGraphContext context);
    }
}