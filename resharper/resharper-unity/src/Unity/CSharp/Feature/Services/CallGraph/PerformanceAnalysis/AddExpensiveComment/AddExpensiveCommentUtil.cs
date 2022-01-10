using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.CallGraph;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis.AddExpensiveComment
{
    public static class AddExpensiveCommentUtil
    {
        public const string MESSAGE = "Mark function expensive";
        public const string COMMENT = "// " + ReSharperControlConstruct.RestorePrefix + " " + ExpensiveCodeMarksProvider.MarkId;
    }
}