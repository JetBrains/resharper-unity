using System.Collections.Generic;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.ContextSystem;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis.ShowExpensiveCalls
{
    [SolutionComponent]
    public class ShowExpensiveCallsCodeInsightProvider : PerformanceCriticalCodeInsightProvider
    {
        private readonly ExpensiveInvocationContextProvider myExpensiveContextProvider;
        private readonly SolutionAnalysisConfiguration myConfiguration;

        public ShowExpensiveCallsCodeInsightProvider(
            ExpensiveInvocationContextProvider expensiveContextProvider, 
            SolutionAnalysisConfiguration configuration,
            ISolution solution) : base(solution)
        {
            myExpensiveContextProvider = expensiveContextProvider;
            myConfiguration = configuration;
        }

        protected override bool CheckCallGraph(IMethodDeclaration methodDeclaration, IReadOnlyCallGraphContext context)
        {
            var callGraphReady = UnityCallGraphUtil.IsCallGraphReady(myConfiguration);
            
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