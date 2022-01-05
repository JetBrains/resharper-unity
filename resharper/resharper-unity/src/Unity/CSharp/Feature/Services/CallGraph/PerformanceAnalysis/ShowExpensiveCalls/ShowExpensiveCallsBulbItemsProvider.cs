using System.Collections.Generic;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.ContextSystem;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis.ShowExpensiveCalls
{
    [SolutionComponent]
    public class ShowExpensiveCallsBulbItemsProvider : PerformanceCriticalBulbItemsProvider
    {
        private readonly ExpensiveInvocationContextProvider myExpensiveContextProvider;
        private readonly SolutionAnalysisConfiguration myConfiguration;

        public ShowExpensiveCallsBulbItemsProvider(
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

        protected override IEnumerable<BulbMenuItem> GetActions(IMethodDeclaration methodDeclaration, ITextControl textControl)
        {
            var actions = ShowExpensiveCallsBulbAction.GetExpensiveCallsActions(methodDeclaration);

            return actions.ToMenuItems(textControl, Solution);
        }
    }
}