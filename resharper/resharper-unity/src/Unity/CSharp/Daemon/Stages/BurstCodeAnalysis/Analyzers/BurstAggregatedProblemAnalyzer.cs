using JetBrains.Application.Components;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers
{
    public abstract class BurstAggregatedProblemAnalyzer<T>(IOrderedImmutableEnumerable<IBurstProblemSubAnalyzer<T>> subAnalyzers)
        : BurstProblemAnalyzerBase<T>
        where T : ITreeNode
    {
        protected sealed override bool CheckAndAnalyze(T t, IHighlightingConsumer consumer, IReadOnlyCallGraphContext context)
        {
            var warningPlaced = false;
            
            foreach (var subAnalyzer in subAnalyzers)
            {
                var res = subAnalyzer.CheckAndAnalyze(t, consumer);

                warningPlaced |= res.IsWarningPlaced();
                
                if (res.IsStop())
                    break;
            }

            return warningPlaced;
        }
    }
}