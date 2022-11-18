using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.CallGraph;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Plugins.Unity.Resources;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis.AddPerformanceAnalysisDisableComment
{
    public sealed class AddPerformanceAnalysisDisableCommentBulbAction : AddCommentBulbActionBase
    {
        public AddPerformanceAnalysisDisableCommentBulbAction([NotNull] IMethodDeclaration methodDeclaration)
            : base(methodDeclaration)
        {
        }

        public override string Text => Strings.AddPerformanceAnalysisDisableCommentContextAction_Name;
        protected override string Comment => "// " + ReSharperControlConstruct.DisablePrefix + " " + UnityCallGraphUtil.PerformanceAnalysisComment;
    }
}