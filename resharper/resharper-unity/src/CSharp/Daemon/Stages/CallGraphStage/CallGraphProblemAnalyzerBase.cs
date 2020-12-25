using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.CallGraphStage
{
    public abstract class CallGraphProblemAnalyzerBase<T> : ICallGraphProblemAnalyzer where T : ITreeNode
    {
        public void RunInspection(ITreeNode node, IHighlightingConsumer consumer, IReadOnlyCallGraphContext context)
        {
            if (node is T t && IsApplicable(context))
                Analyze(t, consumer, context);
        }

        protected abstract bool IsApplicable(IReadOnlyCallGraphContext context);

        protected abstract void Analyze([NotNull] T t,
            [NotNull] IHighlightingConsumer consumer, IReadOnlyCallGraphContext context);
    }
}