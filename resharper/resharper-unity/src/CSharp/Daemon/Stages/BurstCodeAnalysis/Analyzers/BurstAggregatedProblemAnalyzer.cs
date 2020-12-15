using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers
{
    public abstract class BurstAggregatedProblemAnalyzer<T> :  BurstProblemAnalyzerBase<T> where T : ITreeNode
    {
        private readonly List<IBurstProblemSubAnalyzer<T>> mySubAnalyzers;

        protected BurstAggregatedProblemAnalyzer(IEnumerable<IBurstProblemSubAnalyzer<T>> subAnalyzers)
        {
            mySubAnalyzers = subAnalyzers.ToList();
            mySubAnalyzers.Sort((analyzer1, analyzer2) => analyzer1.Priority - analyzer2.Priority);
        }
        
        protected sealed override bool CheckAndAnalyze(T t, IHighlightingConsumer consumer, IReadOnlyContext context)
        {
            var warningPlaced = false;
            
            foreach (var subAnalyzer in mySubAnalyzers)
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