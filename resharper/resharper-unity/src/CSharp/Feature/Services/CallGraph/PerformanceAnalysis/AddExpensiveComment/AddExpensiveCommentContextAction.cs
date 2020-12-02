using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.Analyses.Bulbs;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using static JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis.AddExpensiveComment.ExpensiveCodeActionsUtil;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis.
    AddExpensiveComment
{
    [ContextAction(
        Group = UnityContextActions.GroupID,
        Name = MESSAGE,
        Description = MESSAGE,
        Disabled = false,
        AllowedInNonUserFiles = false,
        Priority = 1)]
    public sealed class AddExpensiveCommentContextAction : PerformanceAnalysisSimpleMethodContextActionBase
    {

        public AddExpensiveCommentContextAction([NotNull] ICSharpContextActionDataProvider dataProvider)
            : base(dataProvider)
        {
        }

        protected override bool ShouldCreate(IMethodDeclaration methodDeclaration, DaemonProcessKind processKind)
        {
            var isExpensiveContext = ExpensiveContextProvider.HasContext(methodDeclaration, processKind);
            var isPerformanceContext = PerformanceContextProvider.HasContext(methodDeclaration, processKind);

            return isPerformanceContext && !isExpensiveContext;
        }
        
        protected override IBulbAction GetBulbAction(IMethodDeclaration methodDeclaration)
        {
            return new AddExpensiveCommentBulbAction(methodDeclaration);
        }
    }
}