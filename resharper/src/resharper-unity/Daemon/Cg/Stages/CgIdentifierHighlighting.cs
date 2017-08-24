using JetBrains.DocumentModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Feature.Services.Daemon;

namespace JetBrains.ReSharper.Plugins.Unity.Daemon.Cg.Stages
{
    [DaemonTooltipProvider(typeof(CgIdentifierTooltipProvider))]
    [StaticSeverityHighlighting(Severity.INFO, HighlightingGroupIds.IdentifierHighlightingsGroup, OverlapResolve = OverlapResolveKind.NONE, ShowToolTipInStatusBar = false)]
    public class CgIdentifierHighlighting : ICustomAttributeIdHighlighting
    {
        private readonly DocumentRange myRange;
        
        public string ToolTip => string.Empty;

        public string ErrorStripeToolTip => ToolTip;
        
        public string AttributeId { get; }

        public CgIdentifierHighlighting(string attributeId, DocumentRange range)
        {
            AttributeId = attributeId;
            myRange = range;
        }

        public bool IsValid() => true;

        public DocumentRange CalculateRange() => myRange;
    }
}