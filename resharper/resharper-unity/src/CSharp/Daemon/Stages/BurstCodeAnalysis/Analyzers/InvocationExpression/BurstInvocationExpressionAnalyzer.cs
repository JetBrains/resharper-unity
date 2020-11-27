using System.Collections.Generic;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers.InvocationExpression
{
    [SolutionComponent]
    public sealed class BurstInvocationExpressionAnalyzer : BurstAggregatedProblemAnalyzer<IInvocationExpression>
    {
        public BurstInvocationExpressionAnalyzer(
            IEnumerable<IBurstProblemSubAnalyzer<IInvocationExpression>> subAnalyzers)
            : base(subAnalyzers)
        {
        }
    }
}