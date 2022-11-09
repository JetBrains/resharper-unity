using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.CallGraph;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis.AddExpensiveComment
{
    public sealed class AddExpensiveCommentBulbAction : AddCommentBulbActionBase
    {
        public AddExpensiveCommentBulbAction([NotNull] IMethodDeclaration methodDeclaration)
            : base(methodDeclaration)
        {
        }

        public override string Text => Strings.AddExpensiveCommentContextAction_Name;
        protected override string Comment => "// " + ReSharperControlConstruct.RestorePrefix + " " + ExpensiveCodeMarksProvider.MarkId;
    }
}