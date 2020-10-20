using System.Collections.Generic;
using System.Linq;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers.InvocationExpression
{
    [SolutionComponent]
    public class BurstInvocationExpressionAnalyzer : BurstProblemAnalyzerBase<IInvocationExpression>
    {
        private readonly List<IBurstProblemSubAnalyzer<IInvocationExpression>> mySubAnalyzers;

        public BurstInvocationExpressionAnalyzer(
            IEnumerable<IBurstProblemSubAnalyzer<IInvocationExpression>> subAnalyzers)
        {
            mySubAnalyzers = subAnalyzers.ToList();
            mySubAnalyzers.Sort((analyzer1, analyzer2) => analyzer1.Priority - analyzer2.Priority);
        }
        
        protected override bool CheckAndAnalyze(IInvocationExpression invocationExpression, IHighlightingConsumer consumer)
        {
            var warningPlaced = false;
            
            foreach (var subAnalyzer in mySubAnalyzers)
            {
                var res = subAnalyzer.CheckAndAnalyze(invocationExpression, consumer);

                warningPlaced |= res.IsWarningPlaced();
                
                if (res.IsStop())
                    break;
            }

            return warningPlaced;
        }
    }
}