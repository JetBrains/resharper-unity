using System.Collections.Generic;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.Rider;
using JetBrains.ReSharper.Plugins.Unity.Rider.Highlightings.IconsProviders;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis.ShowExpensiveCalls.Rider
{
    [SolutionComponent]
    public class ShowExpensiveCallsCodeInsightProvider : SimpleCodeVisionMenuItemProviderBase, IPerformanceAnalysisCodeInsightMenuItemProvider
    {
        private readonly ExpensiveInvocationContextProvider myExpensiveContextProvider;
        private readonly SolutionAnalysisService myService;

        public ShowExpensiveCallsCodeInsightProvider(ExpensiveInvocationContextProvider expensiveContextProvider, ISolution solution, SolutionAnalysisService service) : base(solution)
        {
            myExpensiveContextProvider = expensiveContextProvider;
            myService = service;
        }

        protected override bool CheckCallGraph(IMethodDeclaration methodDeclaration, IReadOnlyCallGraphContext context)
        {
            var declaredElement = methodDeclaration.DeclaredElement;
            
            return myExpensiveContextProvider.IsMarkedSweaDependent(declaredElement, myService);
        }

        protected override IEnumerable<IBulbAction> GetActions(IMethodDeclaration methodDeclaration)
        {
            var actions = ShowExpensiveCallsBulbAction.GetAllCalls(methodDeclaration);

            return actions;
        }
    }
}