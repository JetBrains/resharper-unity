using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers
{
    public interface IBurstProblemSubAnalyzer<in T> where T : ITreeNode
    {
        BurstProblemSubAnalyzerStatus CheckAndAnalyze(T referenceExpression, IHighlightingConsumer consumer);

        /// <summary>
        /// Analyzer priority.
        /// </summary>
        int Priority { get; }
    }

    public enum BurstProblemSubAnalyzerStatus : byte
    {
        // ReSharper disable InconsistentNaming
        WARNING_PLACED_STOP,
        WARNING_PLACED_CONTINUE,
        NO_WARNING_STOP,
        NO_WARNING_CONTINUE
        // ReSharper enable InconsistentNaming
    }

    public static class BurstProblemSubAnalyzerStatusUtil
    {
        public static bool IsStop(this BurstProblemSubAnalyzerStatus status)
        {
            return status == BurstProblemSubAnalyzerStatus.NO_WARNING_STOP ||
                   status == BurstProblemSubAnalyzerStatus.WARNING_PLACED_STOP;
        }

        public static bool IsWarningPlaced(this BurstProblemSubAnalyzerStatus status)
        {
            return status == BurstProblemSubAnalyzerStatus.WARNING_PLACED_STOP ||
                   status == BurstProblemSubAnalyzerStatus.WARNING_PLACED_CONTINUE;
        }
    }
}