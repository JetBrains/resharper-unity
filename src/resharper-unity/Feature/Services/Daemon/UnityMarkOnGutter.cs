using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Feature.Services.Daemon
{
    [StaticSeverityHighlighting(Severity.INFO, HighlightingGroupIds.GutterMarksGroup, OverlapResolve = OverlapResolveKind.NONE, AttributeId = HighlightingAttributeIds.UNITY_GUTTER_ICON_ATTRIBUTE)]
    public class UnityMarkOnGutter : IHighlighting
    {
        private readonly ITreeNode myElement;
        private readonly DocumentRange myRange;

        public UnityMarkOnGutter(ITreeNode element, DocumentRange range, string tooltip)
        {
            myElement = element;
            myRange = range;
            ToolTip = tooltip;
        }

        public bool IsValid()
        {
            return myElement == null || myElement.IsValid();
        }

        public DocumentRange CalculateRange() => myRange;
        public string ToolTip { get; }
        public string ErrorStripeToolTip => ToolTip;
    }
}