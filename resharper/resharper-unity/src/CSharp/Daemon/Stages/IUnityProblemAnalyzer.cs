using JetBrains.Annotations;
using JetBrains.ReSharper.Daemon.CallGraph;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Analyzers
{
    public interface IUnityProblemAnalyzer
    {
        UnityProblemAnalyzerContext Context { get; }
        void RunInspection(ITreeNode node, IDaemonProcess daemonProcess, DaemonProcessKind kind, [NotNull] IHighlightingConsumer consumer);
    }
}