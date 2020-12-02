using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.CallGraph;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis
{
    public sealed class PerformanceAnalysisDisableByCommentBulbAction : AddCommentActionBulbBase
    {
        public PerformanceAnalysisDisableByCommentBulbAction([NotNull] IMethodDeclaration methodDeclaration)
            : base(methodDeclaration)
        {
        }
        
        protected override string Comment => "// " + ReSharperControlConstruct.DisablePrefix + " " +
                                             UnityCallGraphUtil.PerformanceExpensiveComment;
        public override string Text => PerformanceDisableUtil.MESSAGE;
    }
}