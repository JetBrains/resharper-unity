using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis.AddPerformanceAnalysisDisableComment
{
    public sealed class AddPerformanceAnalysisDisableCommentBulbAction : AddCommentBulbActionBase
    {
        public AddPerformanceAnalysisDisableCommentBulbAction([NotNull] IMethodDeclaration methodDeclaration)
            : base(methodDeclaration)
        {
        }

        public override string Text => AddPerformanceAnalysisDisableCommentUtil.MESSAGE;
        protected override string Comment => AddPerformanceAnalysisDisableCommentUtil.COMMENT;
    }
}