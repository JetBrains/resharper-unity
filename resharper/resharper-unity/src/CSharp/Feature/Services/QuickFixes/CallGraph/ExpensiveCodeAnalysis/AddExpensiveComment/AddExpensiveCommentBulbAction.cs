using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.CallGraph;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph.ExpensiveCodeAnalysis.
    AddExpensiveComment
{
    public sealed class AddExpensiveCommentBulbAction : AddCommentActionBulbBase
    {
        public AddExpensiveCommentBulbAction([NotNull] IMethodDeclaration methodDeclaration)
            : base(methodDeclaration)
        {
        }

        protected override string Comment =>
            "// " + ReSharperControlConstruct.RestorePrefix + " " + ExpensiveCodeMarksProvider.MarkId;

        public override string Text => ExpensiveCodeActionsUtil.MESSAGE;
    }
}