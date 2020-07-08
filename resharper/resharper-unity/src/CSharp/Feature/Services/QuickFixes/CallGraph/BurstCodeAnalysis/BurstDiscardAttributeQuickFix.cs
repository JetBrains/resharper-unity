using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph.BurstCodeAnalysis
{
    [QuickFix]
    public sealed class BurstDiscardAttributeQuickFix : BurstAnalysisContextActionBase, IQuickFix
    {
        public BurstDiscardAttributeQuickFix(IBurstHighlighting burstHighlighting)
            : base(burstHighlighting)
        {
        }

        protected override IClrTypeName ProtagonistAttribute => KnownTypes.BurstDiscardAttribute;
        protected override IClrTypeName AntagonistAttribute => null;
        
        public override string Text => "Add BurstDiscard attribute";
    }
}