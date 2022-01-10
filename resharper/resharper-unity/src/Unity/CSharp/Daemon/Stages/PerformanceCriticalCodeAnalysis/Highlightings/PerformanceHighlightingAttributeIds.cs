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
        Layer = HighlighterLayer.ADDITIONAL_SYNTAX)]
    [RegisterHighlighter(NULL_COMPARISON,
        GroupId = UnityHighlightingAttributeIds.GROUP_ID,
        EffectType = EffectType.SOLID_UNDERLINE,
        EffectColor = "#ff7526",
        NotRecyclable = true,
        Layer = HighlighterLayer.ADDITIONAL_SYNTAX)]
    [RegisterHighlighter(CAMERA_MAIN,
        GroupId = UnityHighlightingAttributeIds.GROUP_ID,
        EffectColor = "#ff7526",
        NotRecyclable = true,
        EffectType = EffectType.SOLID_UNDERLINE,
        Layer = HighlighterLayer.ADDITIONAL_SYNTAX)]
    [RegisterHighlighter(INEFFICIENT_MULTIDIMENSIONAL_ARRAYS_USAGE,
        GroupId = UnityHighlightingAttributeIds.GROUP_ID,
        EffectColor = "#ff7526",
        NotRecyclable = true,
        EffectType = EffectType.SOLID_UNDERLINE,
        Layer = HighlighterLayer.ADDITIONAL_SYNTAX)]
    [RegisterHighlighter(INEFFICIENT_MULTIPLICATION_ORDER,
        GroupId = UnityHighlightingAttributeIds.GROUP_ID,
        EffectColor = "#ff7526",
        NotRecyclable = true,
        EffectType = EffectType.SOLID_UNDERLINE,
        Layer = HighlighterLayer.ADDITIONAL_SYNTAX)]
    // Note that PERFORMANCE_CRITICAL_METHOD_HIGHLIGHTER is registered separately for Rider and ReSharper, because VS/R#
    // don't support EffectType.LINE_MARKER
    public static class PerformanceHighlightingAttributeIds
    {
        // All attribute IDs should begin "ReSharper Unity ". See UnityHighlighterNamesProvider
        public const string CAMERA_MAIN = "ReSharper Unity Expensive Camera Main Usage";
        public const string COSTLY_METHOD_INVOCATION = "ReSharper Unity Expensive Method Invocation";
        public const string NULL_COMPARISON = "ReSharper Unity Expensive Null Comparison";
        public const string INEFFICIENT_MULTIPLICATION_ORDER = "ReSharper Unity Inefficient Multiplication Order";
        public const string INEFFICIENT_MULTIDIMENSIONAL_ARRAYS_USAGE = "ReSharper Unity Inefficient Multidimensional Array Usage";
        public const string PERFORMANCE_CRITICAL_METHOD_HIGHLIGHTER = "ReSharper Unity Performance Critical Line Marker";
    }
}