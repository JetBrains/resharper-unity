using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph.BurstCodeAnalysis
{
    [QuickFix]
    public sealed class BurstDiscardAttributeQuickFix : BurstDiscardAttributeAction
    {
        public BurstDiscardAttributeQuickFix(IBurstHighlighting burstHighlighting)
            : base(burstHighlighting)
        {
        }
    }
}