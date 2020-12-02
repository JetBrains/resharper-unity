using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis.PerformanceAnalysisDisableByComment
{
    public struct PerformanceAnalysisDisableByCommentBulbActionProvider : IBulbActionProvider<PerformanceAnalysisDisableByCommentBulbAction>
    {
        public PerformanceAnalysisDisableByCommentBulbAction GetBulbAction(IMethodDeclaration methodDeclaration)
        {
            return new PerformanceAnalysisDisableByCommentBulbAction(methodDeclaration);
        }
    }

    [QuickFix]
    public sealed class PerformanceAnalysisDisableByCommentQuickFix : ContainingMethodQuickFixBase<
        PerformanceAnalysisDisableByCommentBulbActionProvider, PerformanceAnalysisDisableByCommentBulbAction>, IQuickFix
    {
        public PerformanceAnalysisDisableByCommentQuickFix(UnityPerformanceInvocationWarning performanceHighlighting)
            : base(performanceHighlighting?.InvocationExpression)
        {
        }

        public bool IsAvailable(IUserDataHolder cache)
        {
            return PerformanceDisableUtil.IsAvailable(MethodDeclaration);
        }
    }
}