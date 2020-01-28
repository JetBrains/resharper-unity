using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.TextControl.DocumentMarkup;
using JetBrains.TextControl.DocumentMarkup.LineMarkers;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Highlightings
{
    [StaticSeverityHighlighting(Severity.INFO, CSharpLanguage.Name, Languages = "CSHARP",
        AttributeId = PerformanceHighlightingAttributeIds.PERFORMANCE_CRITICAL_METHOD_HIGHLIGHTER,
        ShowToolTipInStatusBar = false,
        ToolTipFormatString = MESSAGE)]
    public class UnityPerformanceCriticalCodeLineMarker: UnityPerformanceHighlightingBase, IHighlighting , IActiveLineMarkerInfo
    {
        public const string SEVERITY_ID = "Unity.PerformanceCriticalCodeHighlighting";
        public const string TITLE = "Performance critical context";
        public const string MESSAGE = "Performance critical context";

        private readonly DocumentRange myRange;

        public UnityPerformanceCriticalCodeLineMarker(DocumentRange range) 
        {
            myRange = range;
        }

        public override bool IsValid() => true;
        public DocumentRange CalculateRange() => myRange;
        public string ToolTip => "Performance critical context";
        public string ErrorStripeToolTip => Tooltip;
        public string RendererId => null;
        public int Thickness => 1;
        public LineMarkerPosition Position =>  LineMarkerPosition.RIGHT;
        public ExecutableItem LeftClick() => null;
        public string Tooltip => "Performance critical context";
    }

    public class UnityPerformanceContextHighlightInfo : HighlightInfo
    {
        public UnityPerformanceContextHighlightInfo(DocumentRange documentRange)
            : base(PerformanceHighlightingAttributeIds.PERFORMANCE_CRITICAL_METHOD_HIGHLIGHTER, documentRange, AreaType.EXACT_RANGE, HighlighterLayer.SYNTAX + 1)
        {
        }

        public override IHighlighter CreateHighlighter(IDocumentMarkup markup)
        {
            var highlighter = base.CreateHighlighter(markup);
            highlighter.UserData = new UnityPerformanceCriticalCodeLineMarker(DocumentRange.InvalidRange);
            return highlighter;
        }

    }
}