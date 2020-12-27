using System.Collections.Generic;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.ContextSystem;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis.ShowExpensiveCalls
{
    [SolutionComponent]
    public class ShowExpensiveCallsCodeInsightProvider : SimpleCodeInsightMenuItemProviderBase, IPerformanceAnalysisCodeInsightMenuItemProvider
    {
        private readonly ExpensiveInvocationContextProvider myExpensiveContextProvider;
        public ShowExpensiveCallsCodeInsightProvider(
            ExpensiveInvocationContextProvider expensiveContextProvider, 
            ISolution solution) : base(solution)
        {
            myExpensiveContextProvider = expensiveContextProvider;
        }

        protected override bool CheckCallGraph(IMethodDeclaration methodDeclaration, IReadOnlyCallGraphContext context)
        {
            var callGraphReady = base.CheckCallGraph(methodDeclaration, context);
            
            if (!callGraphReady)
                return false;
            
            var declaredElement = methodDeclaration.DeclaredElement;
            
            return myExpensiveContextProvider.IsMarkedStage(declaredElement, context);
        }

        protected override IEnumerable<IBulbAction> GetActions(IMethodDeclaration methodDeclaration)
        {
            var actions = ShowExpensiveCallsBulbAction.GetAllCalls(methodDeclaration);

            return actions;
        }
    }
}