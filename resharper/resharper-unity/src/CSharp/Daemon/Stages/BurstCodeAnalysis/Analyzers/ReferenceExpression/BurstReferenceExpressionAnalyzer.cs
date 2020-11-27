using System.Collections.Generic;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers.ReferenceExpression
{
    [SolutionComponent]
    public sealed class BurstReferenceExpressionAnalyzer : BurstAggregatedProblemAnalyzer<IReferenceExpression>
    {
        public BurstReferenceExpressionAnalyzer(
            IEnumerable<IBurstProblemSubAnalyzer<IReferenceExpression>> subAnalyzers)
            : base(subAnalyzers)
        {
        }
    }
}