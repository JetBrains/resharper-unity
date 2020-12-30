using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.ContextActions;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using static JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis.PerformanceAnalysisDisableByComment.PerformanceDisableUtil;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis.PerformanceAnalysisDisableByComment
{
    [ContextAction(
        Group = UnityContextActions.GroupID,
        Name = MESSAGE,
        Description = MESSAGE,
        Disabled = false,
        AllowedInNonUserFiles = false,
        Priority = 1)]
    public sealed class PerformanceAnalysisDisableByCommentContextAction : PerformanceAnalysisSimpleMethodContextActionBase
    {
        public PerformanceAnalysisDisableByCommentContextAction([NotNull] ICSharpContextActionDataProvider dataProvider)
            : base(dataProvider)
        {
        }
        
        protected override bool ShouldCreate(IMethodDeclaration methodDeclaration, DaemonProcessKind processKind)
        {
            var isExpensiveContext = ExpensiveContextProvider.HasContext(methodDeclaration, processKind);

            return isExpensiveContext || PerformanceContextProvider.HasContext(methodDeclaration, processKind);
        }

        protected override IBulbAction GetBulbAction(IMethodDeclaration methodDeclaration)
        {
            return new PerformanceAnalysisDisableByCommentBulbAction(methodDeclaration);
        }
    }
}