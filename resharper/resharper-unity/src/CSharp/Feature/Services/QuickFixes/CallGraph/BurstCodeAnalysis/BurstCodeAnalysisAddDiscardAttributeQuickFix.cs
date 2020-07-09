using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph.BurstCodeAnalysis
{
    [QuickFix]
    public sealed class BurstCodeAnalysisAddDiscardAttributeQuickFix : BurstCodeAnalysisAddDiscardAttributeAction
    {
        public BurstCodeAnalysisAddDiscardAttributeQuickFix(IBurstHighlighting burstHighlighting)
            : base(burstHighlighting)
        {
        }
    }
}