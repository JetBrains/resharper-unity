using JetBrains.Application.Components;
using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers.ReferenceExpression
{
    [SolutionComponent(Instantiation.DemandAnyThreadSafe)]
    public sealed class BurstReferenceExpressionAnalyzer(IOrderedImmutableEnumerable<IBurstProblemSubAnalyzer<IReferenceExpression>> subAnalyzers)
        : BurstAggregatedProblemAnalyzer<IReferenceExpression>(subAnalyzers), IBurstBannedAnalyzer;
}