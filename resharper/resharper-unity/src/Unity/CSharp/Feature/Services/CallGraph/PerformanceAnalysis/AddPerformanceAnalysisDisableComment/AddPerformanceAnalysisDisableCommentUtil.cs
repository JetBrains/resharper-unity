using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.CallGraph;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis.AddPerformanceAnalysisDisableComment
{
    public static class AddPerformanceAnalysisDisableCommentUtil
    {
        public const string MESSAGE = "Disable performance analysis for method";
        public const string COMMENT = "// " + ReSharperControlConstruct.DisablePrefix + " " + UnityCallGraphUtil.PerformanceAnalysisComment;
    }
}