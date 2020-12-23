using System.Collections.Generic;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.Rider;
using JetBrains.ReSharper.Plugins.Unity.Rider.Highlightings.IconsProviders;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis.AddExpensiveComment.Rider
{  
    [SolutionComponent]
    public class PerformanceAnalysisCodeVisionAddExpensiveProvider : SimpleCodeVisionMenuItemProviderBase, IPerformanceAnalysisCodeVisionMenuItemProvider
    {
        private readonly ExpensiveInvocationContextProvider myExpensiveContextProvider;

        public PerformanceAnalysisCodeVisionAddExpensiveProvider(ExpensiveInvocationContextProvider expensiveContextProvider, ISolution solution) : base(solution)
        {
            myExpensiveContextProvider = expensiveContextProvider;
        }

        protected override bool CheckCallGraph(IMethodDeclaration methodDeclaration, IReadOnlyContext context)
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