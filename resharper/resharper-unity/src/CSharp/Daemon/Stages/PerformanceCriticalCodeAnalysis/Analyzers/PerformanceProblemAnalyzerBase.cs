using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Analyzers
{
    public abstract class PerformanceProblemAnalyzerBase<T> : IPerformanceProblemAnalyzer
    {
        public void RunInspection(ITreeNode node, IDaemonProcess daemonProcess, DaemonProcessKind kind, IHighlightingConsumer consumer)
        {
            if (node is T t)
                Analyze(t, daemonProcess, kind, consumer);
        }


        protected abstract void Analyze(T t, IDaemonProcess daemonProcess, DaemonProcessKind kind, IHighlightingConsumer consumer);
    }
}