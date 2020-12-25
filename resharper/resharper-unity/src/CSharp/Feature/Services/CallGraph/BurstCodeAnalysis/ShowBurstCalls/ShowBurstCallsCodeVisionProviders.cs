using System.Collections.Generic;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.BurstCodeAnalysis.ShowBurstCalls
{
    [SolutionComponent]
    public class ShowBurstCallsCodeInsightProvider: SimpleCodeInsightMenuItemProviderBase, IBurstCodeInsightMenuItemProvider
    {
        public ShowBurstCallsCodeInsightProvider(ISolution solution)
            : base(solution)
        {
        }

        protected override IEnumerable<IBulbAction> GetActions(IMethodDeclaration methodDeclaration)
        {
            var actions = ShowBurstCallsBulbAction.GetAllCalls(methodDeclaration);

            return actions;
        }
    }
}