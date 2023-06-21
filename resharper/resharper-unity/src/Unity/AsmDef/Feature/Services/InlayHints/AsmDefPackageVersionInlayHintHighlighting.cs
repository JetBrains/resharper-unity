#nullable enable

using JetBrains.DocumentModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Daemon.Attributes;
using JetBrains.ReSharper.Feature.Services.InlayHints;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.TextControl.DocumentMarkup.Adornments;
using JetBrains.UI.RichText;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.InlayHints
{
    // It seems that nearly all inlay hint highlightings use PARAMETER_NAME_HINT
    [DaemonAdornmentProvider(typeof(AsmDefPackageVersionIntraTextAdornmentProvider))]
    [DaemonTooltipProvider(typeof(InlayHintTooltipProvider))]
    [StaticSeverityHighlighting(Severity.INFO,
        typeof(HighlightingGroupIds.IntraTextAdornments),
        AttributeId = AnalysisHighlightingAttributeIds.PARAMETER_NAME_HINT,
        OverlapResolve = OverlapResolveKind.NONE,
        ShowToolTipInStatusBar = false)]
    public class AsmDefPackageVersionInlayHintHighlighting : IAsmDefInlayHintHighlighting
    {
        private readonly DocumentRange myDocumentRange;

        public AsmDefPackageVersionInlayHintHighlighting(DocumentOffset documentOffset, string text, PushToHintMode mode)
        {
            Text = text;
            Mode = mode;
            myDocumentRange = new DocumentRange(documentOffset);
        }

        public string Text { get; }
        public PushToHintMode Mode { get; }
        public string ContextMenuTitle => Strings.AsmDefPackageVersionInlayHintHighlighting_ContextMenuTitle_Package_Version_Hints;
        public bool IsValid() => myDocumentRange.IsValid();
        public DocumentRange CalculateRange() => myDocumentRange;
        public string ToolTip => string.Empty;
        public string ErrorStripeToolTip => string.Empty;
        public RichText Description => RichText.Empty;
        public string TestOutput => Text;
    }
}