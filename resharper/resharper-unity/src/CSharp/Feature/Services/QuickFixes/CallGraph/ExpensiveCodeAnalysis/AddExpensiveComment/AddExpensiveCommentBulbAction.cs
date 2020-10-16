using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.CallGraph;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph.ExpensiveCodeAnalysis.
    AddExpensiveComment
{
    public class AddExpensiveCommentBulbAction : AddCommentActionBase
    {
        private AddExpensiveCommentBulbAction([NotNull] IMethodDeclaration methodDeclaration)
            : base(methodDeclaration)
        {
        }

        protected override string Comment =>
            "//" + ReSharperControlConstruct.RestorePrefix + " " + ExpensiveCodeMarksProvider.MarkId;

        public override string Text => ExpensiveCodeActionsUtil.MESSAGE;

        [ContractAnnotation("null => null")]
        [ContractAnnotation("notnull => notnull")]
        public static AddExpensiveCommentBulbAction CreateOrNull(
            [CanBeNull] IMethodDeclaration methodDeclaration)
        {
            return methodDeclaration == null
                ? null
                : new AddExpensiveCommentBulbAction(methodDeclaration);
        }
    }
}