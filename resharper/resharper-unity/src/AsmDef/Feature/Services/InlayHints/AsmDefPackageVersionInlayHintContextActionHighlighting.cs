using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Daemon.Attributes;
using JetBrains.ReSharper.Feature.Services.InlayHints;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.InlayHints
{
    // This highlight is for the alt+enter context actions that configure the main inlay highlighting
    [StaticSeverityHighlighting(Severity.INFO,
        typeof(HighlightingGroupIds.IntraTextAdornments),
        AttributeId = AnalysisHighlightingAttributeIds.PARAMETER_NAME_HINT_ACTION,
        OverlapResolve = OverlapResolveKind.NONE,
        ShowToolTipInStatusBar = false)]
    public class AsmDefPackageVersionInlayHintContextActionHighlighting : IAsmDefInlayHintContextActionHighlighting
    {
        private readonly DocumentRange myDocumentRange;

        public AsmDefPackageVersionInlayHintContextActionHighlighting(DocumentRange documentRange)
        {
            myDocumentRange = documentRange;
            BulbActionsProvider = new AsmDefPackageVersionInlayHintBulbActionsProvider();
        }

        public bool IsValid() => myDocumentRange.IsValid();
        public DocumentRange CalculateRange() => myDocumentRange;

        public string ToolTip => string.Empty;
        public string ErrorStripeToolTip => string.Empty;
        public string TestOutput => GetType().Name;
        public IInlayHintBulbActionsProvider BulbActionsProvider { get; }
    }
}