using System.Collections.Generic;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis.AddPerformanceAnalysisDisableComment
{
    [SolutionComponent]
    public sealed class AddDisableCommentCodeInsightProvider : PerformanceCriticalCodeInsightProvider
    {
        public AddDisableCommentCodeInsightProvider(ISolution solution) : base(solution)
        {
        }
        
        protected override IEnumerable<IBulbAction> GetActions(IMethodDeclaration methodDeclaration)
        {
            var action = new AddPerformanceAnalysisDisableCommentBulbAction(methodDeclaration);

            return new[] {action};
        }
    }
}