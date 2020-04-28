using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using JetBrains.TextControl.DocumentMarkup;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Highlightings
{
    // Note that these are registered as part of the main "Unity" group, as they should appear in the main "Unity"
    // Color Scheme page
    [RegisterHighlighter(COSTLY_METHOD_INVOCATION,
        GroupId = UnityHighlightingAttributeIds.GROUP_ID,
        EffectType = EffectType.SOLID_UNDERLINE,
        EffectColor = "#ff7526",
        NotRecyclable = true,
        Layer = HighlighterLayer.SYNTAX)]
    [RegisterHighlighter(NULL_COMPARISON,
        GroupId = UnityHighlightingAttributeIds.GROUP_ID,
        EffectType = EffectType.SOLID_UNDERLINE,
        EffectColor = "#ff7526",
        NotRecyclable = true,
        Layer = HighlighterLayer.SYNTAX)]
    [RegisterHighlighter(CAMERA_MAIN,
        GroupId = UnityHighlightingAttributeIds.GROUP_ID,
        EffectColor = "#ff7526",
        NotRecyclable = true,
        EffectType = EffectType.SOLID_UNDERLINE,
        Layer = HighlighterLayer.SYNTAX)]
    [RegisterHighlighter(INEFFICIENT_MULTIDIMENSIONAL_ARRAYS_USAGE,
        GroupId = UnityHighlightingAttributeIds.GROUP_ID,
        EffectColor = "#ff7526",
        NotRecyclable = true,
        EffectType = EffectType.SOLID_UNDERLINE,
        Layer = HighlighterLayer.SYNTAX)]
    [RegisterHighlighter(INEFFICIENT_MULTIPLICATION_ORDER,
        GroupId = UnityHighlightingAttributeIds.GROUP_ID,
        EffectColor = "#ff7526",
        NotRecyclable = true,
        EffectType = EffectType.SOLID_UNDERLINE,
        Layer = HighlighterLayer.SYNTAX)]
        // ReSharper doesn't currently support EffectType.LINE_MARKER, so we'll use SOLID_UNDERLINE on the method name
        // instead. Make sure the range is updated in any usages when this is removed!
#if RIDER
    [RegisterHighlighter(PERFORMANCE_CRITICAL_METHOD_HIGHLIGHTER,
        GroupId = UnityHighlightingAttributeIds.GROUP_ID,
        BackgroundColor = "#ff7526",
        DarkBackgroundColor = "#ff7526",
        EffectType = EffectType.LINE_MARKER,
        Layer = HighlighterLayer.CARET_ROW - 1,
        TransmitUpdates = true)]
#else
    [RegisterHighlighter(PERFORMANCE_CRITICAL_METHOD_HIGHLIGHTER,
        GroupId = UnityHighlightingAttributeIds.GROUP_ID,
        EffectColor = "#ff7526",
        EffectType = EffectType.SOLID_UNDERLINE,
        Layer = HighlighterLayer.WARNING + 1)]
#endif
    public static class PerformanceHighlightingAttributeIds
    {
        public const string CAMERA_MAIN = "ReSharper Unity Expensive Camera Main Usage";
        public const string COSTLY_METHOD_INVOCATION = "ReSharper Unity Expensive Method Invocation";
        public const string NULL_COMPARISON = "ReSharper Unity Expensive Null Comparison";
        public const string INEFFICIENT_MULTIPLICATION_ORDER = "ReSharper Unity Inefficient Multiplication Order";
        public const string INEFFICIENT_MULTIDIMENSIONAL_ARRAYS_USAGE = "ReSharper Unity Inefficient Multidimensional Array Usage";
        public const string PERFORMANCE_CRITICAL_METHOD_HIGHLIGHTER = "ReSharper Unity Performance Critical Line Marker";
    }
}