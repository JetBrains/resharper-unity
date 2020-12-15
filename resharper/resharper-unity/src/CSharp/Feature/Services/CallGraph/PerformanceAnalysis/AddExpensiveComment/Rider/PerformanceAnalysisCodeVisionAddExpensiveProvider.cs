using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.Rider.Highlightings.IconsProviders;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis.AddExpensiveComment.Rider
{  
    [SolutionComponent]
    public class PerformanceAnalysisCodeVisionAddExpensiveProvider : IPerformanceAnalysisCodeVisionMenuItemProvider
    {
        private readonly ExpensiveInvocationContextProvider myExpensiveContextProvider;
        private readonly ISolution mySolution;

        public PerformanceAnalysisCodeVisionAddExpensiveProvider(ExpensiveInvocationContextProvider expensiveContextProvider, ISolution solution)
        {
            myExpensiveContextProvider = expensiveContextProvider;
            mySolution = solution;
        }

        public BulbMenuItem GetMenuItem(IMethodDeclaration methodDeclaration, ITextControl textControl, DaemonProcessKind processKind)
        {
            if (myExpensiveContextProvider.IsMarkedStage(methodDeclaration, processKind))
                return null;
            
            var bulb = new AddExpensiveCommentBulbAction(methodDeclaration);
            var item = UnityCallGraphUtil.BulbActionToMenuItem(bulb, textControl, mySolution, BulbThemedIcons.ContextAction.Id);

            return item;
        }
    }
}