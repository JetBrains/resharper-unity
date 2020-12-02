using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis.AddExpensiveComment
{
    public sealed class AddExpensiveCommentBulbAction : AddCommentActionBulbBase
    {
        public AddExpensiveCommentBulbAction([NotNull] IMethodDeclaration methodDeclaration)
            : base(methodDeclaration, 
                comment: ExpensiveCodeActionsUtil.COMMENT,
                text: ExpensiveCodeActionsUtil.MESSAGE)
        {
        }
    }
}