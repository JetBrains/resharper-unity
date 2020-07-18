using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl.DocumentMarkup;
using JetBrains.TextControl.DocumentMarkup.LineMarkers;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Highlightings
{
    [StaticSeverityHighlighting(Severity.INFO,
        typeof(UnityBurstHighlighting),
        Languages = CSharpLanguage.Name,
        AttributeId = BurstCodeAnalysisHighlightingsAttributesIds.BURST_METHOD_HIGHLIGHTER,
        ShowToolTipInStatusBar = false,
        ToolTipFormatString = MESSAGE)]
    public class UnityBurstCodeLineMarker : IBurstHighlighting, IActiveLineMarkerInfo
    {
        public const string MESSAGE = "Burst context";

        private readonly DocumentRange myRange;

        public UnityBurstCodeLineMarker(DocumentRange range)
        {
            myRange = range;
        }

        public bool IsValid() => true;
        public DocumentRange CalculateRange() => myRange;
        public string ToolTip => "Burst context";
        public string ErrorStripeToolTip => Tooltip;
        public string RendererId => null;
        public int Thickness => 1;
        public LineMarkerPosition Position =>  LineMarkerPosition.RIGHT;
        public ExecutableItem LeftClick() => null;
        public string Tooltip => "Burst context";
        public ITreeNode Node => null;
    }
    

    public class UnityBurstContextHighlightInfo : HighlightInfo
    {
        public UnityBurstContextHighlightInfo(DocumentRange documentRange)
            : base(BurstCodeAnalysisHighlightingsAttributesIds.BURST_METHOD_HIGHLIGHTER, documentRange, AreaType.EXACT_RANGE, HighlighterLayer.SYNTAX + 1)
        {        
        }

        public override IHighlighter CreateHighlighter(IDocumentMarkup markup)
        {
            var highlighter = base.CreateHighlighter(markup);
            highlighter.UserData = new UnityBurstCodeLineMarker(DocumentRange);
            return highlighter;
        }
    }
#if RIDER
    [RegisterHighlighter(BURST_METHOD_HIGHLIGHTER,
        GroupId = UnityHighlightingAttributeIds.GROUP_ID,
        BackgroundColor = "#ff7526",
        DarkBackgroundColor = "#ff7526",
        EffectType = EffectType.LINE_MARKER,
        EffectColor = "#ff7526",
        Layer = HighlighterLayer.ADDITIONAL_SYNTAX,
        TransmitUpdates = true)]
#else
    [RegisterHighlighter(BURST_METHOD_HIGHLIGHTER,
        GroupId = UnityHighlightingAttributeIds.GROUP_ID,
        EffectColor = "#ff7526",
        EffectType = EffectType.SOLID_UNDERLINE,
        Layer = HighlighterLayer.WARNING + 1)]
#endif
    public static class BurstCodeAnalysisHighlightingsAttributesIds
    {
        public const string BURST_METHOD_HIGHLIGHTER = "ReSharper Unity Burst Code Line Marker";
    }
}