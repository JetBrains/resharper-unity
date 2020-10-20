using System.Collections.Generic;
using System.Linq;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers.ReferenceExpression
{
    [SolutionComponent]
    public class BurstReferenceExpressionAnalyzer : BurstProblemAnalyzerBase<IReferenceExpression>
    {
        private readonly List<IBurstProblemSubAnalyzer<IReferenceExpression>> mySubAnalyzers;

        public BurstReferenceExpressionAnalyzer(
            IEnumerable<IBurstProblemSubAnalyzer<IReferenceExpression>> subAnalyzers)
        {
            mySubAnalyzers = subAnalyzers.ToList();
            mySubAnalyzers.Sort((analyzer1, analyzer2) => analyzer1.Priority - analyzer2.Priority);
        }
        
        protected override bool CheckAndAnalyze(IReferenceExpression referenceExpression, IHighlightingConsumer consumer)
        {
            var warningPlaced = false;
            
            foreach (var subAnalyzer in mySubAnalyzers)
            {
                var res = subAnalyzer.CheckAndAnalyze(referenceExpression, consumer);

                warningPlaced |= res.IsWarningPlaced();
                
                if (res.IsStop())
                    break;
            }

            return warningPlaced;
        }
    }
}