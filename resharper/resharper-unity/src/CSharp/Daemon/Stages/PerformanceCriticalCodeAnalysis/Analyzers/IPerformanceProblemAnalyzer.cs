using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Analyzers
{
    public interface IPerformanceProblemAnalyzer
    {
        void RunInspection(ITreeNode node, IDaemonProcess daemonProcess, DaemonProcessKind kind, IHighlightingConsumer consumer);
    }
}