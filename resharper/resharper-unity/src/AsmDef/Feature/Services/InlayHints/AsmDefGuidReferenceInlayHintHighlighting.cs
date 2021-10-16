using JetBrains.Application.InlayHints;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Daemon.Attributes;
using JetBrains.ReSharper.Feature.Services.InlayHints;
using JetBrains.UI.RichText;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.InlayHints
{
    // It seems that nearly all inlay hint highlightings use PARAMETER_NAME_HINT
    [DaemonIntraTextAdornmentProvider(typeof(AsmDefGuidReferenceIntraTextAdornmentProvider))]
    [DaemonTooltipProvider(typeof(InlayHintTooltipProvider))]
    [StaticSeverityHighlighting(Severity.INFO,
        typeof(HighlightingGroupIds.IntraTextAdornments),
        AttributeId = AnalysisHighlightingAttributeIds.PARAMETER_NAME_HINT,
        OverlapResolve = OverlapResolveKind.NONE,
        ShowToolTipInStatusBar = false)]
    public class AsmDefGuidReferenceInlayHintHighlighting : IAsmDefInlayHintHighlighting
    {
        private readonly DocumentRange myDocumentRange;

        public AsmDefGuidReferenceInlayHintHighlighting(DocumentOffset documentOffset, string text, InlayHintsMode mode)
        {
            Text = text;
            Mode = mode;
            myDocumentRange = new DocumentRange(documentOffset);
        }

        public string Text { get; }
        public InlayHintsMode Mode { get; }
        public string ContextMenuTitle => "GUID Reference Hints";
        public bool IsValid() => myDocumentRange.IsValid();
        public DocumentRange CalculateRange() => myDocumentRange;
        public string ToolTip => string.Empty;
        public string ErrorStripeToolTip => string.Empty;
        public RichText Description => RichText.Empty;
        public string TestOutput => Text;
    }
}