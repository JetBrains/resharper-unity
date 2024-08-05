using System.Collections.Generic;
using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers.ReferenceExpression
{
    [SolutionComponent(InstantiationEx.LegacyDefault)]
    public sealed class BurstReferenceExpressionAnalyzer : BurstAggregatedProblemAnalyzer<IReferenceExpression>, IBurstBannedAnalyzer
    {
        public BurstReferenceExpressionAnalyzer(
            IEnumerable<IBurstProblemSubAnalyzer<IReferenceExpression>> subAnalyzers)
            : base(subAnalyzers)
        {
        }
    }
}