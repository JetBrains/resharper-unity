using System.Collections.Generic;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.BurstCodeAnalysis.AddDiscardAttribute
{
    [SolutionComponent]
    public class AddDiscardCodeInsightProvider : BurstCodeInsightProvider
    {
        public AddDiscardCodeInsightProvider(ISolution solution)
            : base(solution)
        {
        }


        protected override IEnumerable<IBulbAction> GetActions(IMethodDeclaration methodDeclaration)
        {
            var action = new AddDiscardAttributeBulbAction(methodDeclaration);

            return new[] {action};
        }
    }
}