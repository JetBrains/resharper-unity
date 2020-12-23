using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.CallGraphStage
{
    public interface ICallGraphProblemAnalyzer
    {
        void RunInspection([NotNull] ITreeNode node, [NotNull] IHighlightingConsumer consumer, [NotNull] IReadOnlyContext context);
    }
}