using System.Collections.Generic;
using JetBrains.Application.Parts;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.ContextSystem;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis.AddExpensiveComment
{
    [SolutionComponent(Instantiation.DemandAnyThread)]
    public class AddExpensiveCommentBulbItemsProvider : PerformanceCriticalBulbItemsProvider
    {
        private readonly ExpensiveInvocationContextProvider myExpensiveContextProvider;

        public AddExpensiveCommentBulbItemsProvider(ExpensiveInvocationContextProvider expensiveContextProvider, PerformanceCriticalContextProvider performanceCriticalContextProvider, ISolution solution)
            : base(solution, performanceCriticalContextProvider)
        {
            myExpensiveContextProvider = expensiveContextProvider;
        }

        protected override bool CheckCallGraph(IMethodDeclaration methodDeclaration, IReadOnlyCallGraphContext context)
        {
            var declaredElement = methodDeclaration.DeclaredElement;

            return !myExpensiveContextProvider.IsMarkedStage(declaredElement, context)
                   && base.CheckCallGraph(methodDeclaration, context);
        }

        protected override IEnumerable<BulbMenuItem> GetActions(IMethodDeclaration methodDeclaration, ITextControl textControl)
        {
            var bulb = new AddExpensiveCommentBulbAction(methodDeclaration);
            var bulbMenuItem = UnityCallGraphUtil.BulbActionToMenuItem(bulb, textControl, Solution, BulbThemedIcons.ContextAction.Id);

            return new[] {bulbMenuItem};
        }
    }
}