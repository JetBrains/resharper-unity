using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.CallGraph;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph.PerformanceAnalysis
{
    public class PerformanceAnalysisDisableByCommentBulbAction : AddCommentActionBase
    {
        private PerformanceAnalysisDisableByCommentBulbAction([NotNull] IMethodDeclaration methodDeclaration)
            : base(methodDeclaration)
        {
        }

        [ContractAnnotation("null => null")]
        [ContractAnnotation("notnull => notnull")]
        public static PerformanceAnalysisDisableByCommentBulbAction Create(
            [CanBeNull] IMethodDeclaration methodDeclaration)
        {
            return methodDeclaration == null
                ? null
                : new PerformanceAnalysisDisableByCommentBulbAction(methodDeclaration);
        }

        protected override string Comment => "//" + ReSharperControlConstruct.DisablePrefix + " " +
                                             UnityCallGraphUtil.PerformanceExpensiveComment;
        public override string Text => PerformanceDisableUtil.MESSAGE;
    }
}