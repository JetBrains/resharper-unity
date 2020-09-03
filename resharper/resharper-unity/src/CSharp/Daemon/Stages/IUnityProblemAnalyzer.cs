using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages
{
    public interface IUnityProblemAnalyzer : IUnityProblemAnalyzerContextClassification
    {
        UnityProblemAnalyzerContextElement ProhibitedContext { get; }
        void RunInspection(ITreeNode node, IDaemonProcess daemonProcess, DaemonProcessKind kind, [NotNull] IHighlightingConsumer consumer);
    }
}