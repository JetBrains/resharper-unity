using System.Collections.Generic;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers.Rider;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.Rider;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.BurstCodeAnalysis.ShowBurstCalls.Rider
{
    [SolutionComponent]
    public class ShowBurstCallsCodeVisionProvider: SimpleCodeVisionMenuItemProviderBase, IBurstCodeVisionMenuItemProvider
    {
        public ShowBurstCallsCodeVisionProvider(ISolution solution)
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