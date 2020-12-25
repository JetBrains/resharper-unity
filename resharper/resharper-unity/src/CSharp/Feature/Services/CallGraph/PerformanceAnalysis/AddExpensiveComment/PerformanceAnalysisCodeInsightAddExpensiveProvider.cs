using System.Collections.Generic;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.ContextSystem;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis.AddExpensiveComment
{  
    [SolutionComponent]
    public class PerformanceAnalysisCodeInsightAddExpensiveProvider : SimpleCodeInsightMenuItemProviderBase, IPerformanceAnalysisCodeInsightMenuItemProvider
    {
        private readonly ExpensiveInvocationContextProvider myExpensiveContextProvider;

        public PerformanceAnalysisCodeInsightAddExpensiveProvider(ExpensiveInvocationContextProvider expensiveContextProvider, ISolution solution) : base(solution)
        {
            myExpensiveContextProvider = expensiveContextProvider;
        }

        protected override bool CheckCallGraph(IMethodDeclaration methodDeclaration, IReadOnlyCallGraphContext context)
        {
            var declaredElement = methodDeclaration.DeclaredElement;
            
            return myExpensiveContextProvider.IsMarkedStage(declaredElement, context);
        }
        
        protected override IEnumerable<IBulbAction> GetActions(IMethodDeclaration methodDeclaration)
        {
            var action = new AddExpensiveCommentBulbAction(methodDeclaration);
            return new[] {action};
        }
    }
}