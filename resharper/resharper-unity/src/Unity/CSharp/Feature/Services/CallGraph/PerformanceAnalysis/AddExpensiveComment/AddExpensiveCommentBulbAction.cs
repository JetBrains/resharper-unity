using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis.AddExpensiveComment
{
    public sealed class AddExpensiveCommentBulbAction : AddCommentBulbActionBase
    {
        public AddExpensiveCommentBulbAction([NotNull] IMethodDeclaration methodDeclaration)
            : base(methodDeclaration)
        {
        }

        public override string Text => AddExpensiveCommentUtil.MESSAGE;
        protected override string Comment => AddExpensiveCommentUtil.COMMENT;
    }
}