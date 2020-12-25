using System.Collections.Generic;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis.ShowPerformanceCriticalCalls
{
    [SolutionComponent]
    public class ShowPerformanceCriticalCallsCodeInsightProvider : SimpleCodeInsightMenuItemProviderBase,
        IPerformanceAnalysisCodeInsightMenuItemProvider
    {
        public ShowPerformanceCriticalCallsCodeInsightProvider(ISolution solution)
            : base(solution)
        {
        }

        protected override IEnumerable<IBulbAction> GetActions(IMethodDeclaration methodDeclaration)
        {
            var actions = ShowPerformanceCriticalIncomingCallsBulbAction.GetAllCalls(methodDeclaration);

            return actions;
        }
    }
}