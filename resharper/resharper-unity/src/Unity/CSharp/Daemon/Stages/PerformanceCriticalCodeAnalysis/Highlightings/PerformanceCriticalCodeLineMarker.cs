using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.TextControl.DocumentMarkup;
using JetBrains.TextControl.DocumentMarkup.LineMarkers;
using JetBrains.Util.DataStructures;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Highlightings
{
    [StaticSeverityHighlighting(Severity.INFO,
        typeof(UnityPerformanceHighlighting),
        Languages = CSharpLanguage.Name,
        AttributeId = PerformanceHighlightingAttributeIds.PERFORMANCE_CRITICAL_METHOD_HIGHLIGHTER,
        ShowToolTipInStatusBar = false
        )]
    public class UnityPerformanceCriticalCodeLineMarker : CSharpUnityHighlightingBase, IUnityPerformanceHighlighting, IHighlighting, IActiveLineMarkerInfo
    {
        private readonly DocumentRange myRange;

        public UnityPerformanceCriticalCodeLineMarker(DocumentRange range)
        {
            myRange = range;
        }

        public override bool IsValid() => true;
        public DocumentRange CalculateRange() => myRange;
        public string ToolTip => Strings.UnityPerformanceCriticalCodeLineMarker_Performance_critical_context;
        public string ErrorStripeToolTip => Tooltip;
        public string RendererId => "DefaultLineMarkerRendererProvider";
        public int Thickness => 1;
        public LineMarkerPosition Position =>  LineMarkerPosition.RIGHT;
        public ExecutableItem LeftClick() => null;
        public string Tooltip => Strings.UnityPerformanceCriticalCodeLineMarker_Performance_critical_context;
    }

    public class UnityPerformanceContextHighlightInfo : HighlightInfo
    {
        public UnityPerformanceContextHighlightInfo(DocumentRange documentRange)
            : base(PerformanceHighlightingAttributeIds.PERFORMANCE_CRITICAL_METHOD_HIGHLIGHTER, documentRange, AreaType.EXACT_RANGE, HighlighterLayer.SYNTAX + 1)
        {
        }

        public override IHighlighter CreateHighlighter(IDocumentMarkup markup)
        {
            return markup.AddHighlighterRegistered(null, DocumentRange.TextRange, AreaType, Layer,
                AttributeId, new ErrorStripeAttributes?(),
                null, userdata: new UserDataWrapper(new UnityPerformanceCriticalCodeLineMarker(DocumentRange)));
        }
    }
}
