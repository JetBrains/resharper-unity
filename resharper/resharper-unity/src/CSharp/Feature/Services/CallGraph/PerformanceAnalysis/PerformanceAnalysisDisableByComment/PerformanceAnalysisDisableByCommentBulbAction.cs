using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis.PerformanceAnalysisDisableByComment
{
    public sealed class PerformanceAnalysisDisableByCommentBulbAction : AddCommentActionBulbBase
    {
        public PerformanceAnalysisDisableByCommentBulbAction([NotNull] IMethodDeclaration methodDeclaration)
            : base(methodDeclaration,
             comment: PerformanceDisableUtil.COMMENT,
             text: PerformanceDisableUtil.MESSAGE)
        {
        }
    }
}