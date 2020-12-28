using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.CallGraphStage;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers
{
    public abstract class BurstProblemAnalyzerBase<T> : CallGraphProblemAnalyzerBase<T> where T : ITreeNode
    {
        private const CallGraphContextTag Context = CallGraphContextTag.BURST_CONTEXT;

        protected sealed override bool IsApplicable(IReadOnlyCallGraphContext context)
        {
            return context.IsSuperSetOf(Context);
        }

        protected override void Analyze(T t, IHighlightingConsumer consumer, [NotNull] IReadOnlyCallGraphContext context)
        {
            CheckAndAnalyze(t, consumer, context);
        }

        protected abstract bool CheckAndAnalyze([NotNull] T t, [CanBeNull] IHighlightingConsumer consumer, [CanBeNull] IReadOnlyCallGraphContext context);
        public bool Check(ITreeNode node)
        {
            if (node is T t)
                return CheckAndAnalyze(t, null, null);
            
            return false;
        }
    }
}