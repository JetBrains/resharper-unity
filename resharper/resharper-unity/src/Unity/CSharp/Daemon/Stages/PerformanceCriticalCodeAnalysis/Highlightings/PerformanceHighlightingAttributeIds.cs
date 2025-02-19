using Strings = JetBrains.ReSharper.Plugins.Unity.Resources.Strings;

using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using JetBrains.TextControl.DocumentMarkup;
using JetBrains.TextControl.DocumentMarkup.VisualStudio;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Highlightings
{
    // Note that these are registered as part of the main "Unity" group, as they should appear in the main "Unity"
    // Color Scheme page
    [RegisterHighlighter(COSTLY_METHOD_INVOCATION,
        GroupId = UnityHighlightingAttributeIds.GROUP_ID,
        EffectType = EffectType.SOLID_UNDERLINE,
        ForegroundColor = "#ff7526",
        DarkForegroundColor = "#ff7526",
        NotRecyclable = true,
        RiderPresentableNameResourceType = typeof(Strings),
        RiderPresentableNameResourceName = nameof(Strings.ExpensiveMethodInvocation_RiderPresentableName),
        VsGenerateClassificationDefinition = VsGenerateDefinition.VisibleClassification,
        Layer = HighlighterLayer.ADDITIONAL_SYNTAX)]
    [RegisterHighlighter(NULL_COMPARISON,
        GroupId = UnityHighlightingAttributeIds.GROUP_ID,
        EffectType = EffectType.SOLID_UNDERLINE,
        ForegroundColor = "#ff7526",
        DarkForegroundColor = "#ff7526",
        NotRecyclable = true,
        RiderPresentableNameResourceType = typeof(Strings),
        RiderPresentableNameResourceName = nameof(Strings.ExpensiveNullComparison_RiderPresentableName),
        VsGenerateClassificationDefinition = VsGenerateDefinition.VisibleClassification,
        Layer = HighlighterLayer.ADDITIONAL_SYNTAX)]
    [RegisterHighlighter(CAMERA_MAIN,
        GroupId = UnityHighlightingAttributeIds.GROUP_ID,
        ForegroundColor = "#ff7526",
        DarkForegroundColor = "#ff7526",
        NotRecyclable = true,
        EffectType = EffectType.SOLID_UNDERLINE,
        RiderPresentableNameResourceType = typeof(Strings),
        RiderPresentableNameResourceName = nameof(Strings.ExpensiveCameraMainUsage_RiderPresentableName),
        VsGenerateClassificationDefinition = VsGenerateDefinition.VisibleClassification,
        Layer = HighlighterLayer.ADDITIONAL_SYNTAX)]
    [RegisterHighlighter(INEFFICIENT_MULTIDIMENSIONAL_ARRAYS_USAGE,
        GroupId = UnityHighlightingAttributeIds.GROUP_ID,
        ForegroundColor = "#ff7526",
        DarkForegroundColor = "#ff7526",
        NotRecyclable = true,
        EffectType = EffectType.SOLID_UNDERLINE,
        RiderPresentableNameResourceType = typeof(Strings),
        RiderPresentableNameResourceName = nameof(Strings.InefficientMultidimensionalArrayUsage_RiderPresentableName),
        VsGenerateClassificationDefinition = VsGenerateDefinition.VisibleClassification,
        Layer = HighlighterLayer.ADDITIONAL_SYNTAX)]
    [RegisterHighlighter(INEFFICIENT_MULTIPLICATION_ORDER,
        GroupId = UnityHighlightingAttributeIds.GROUP_ID,
        ForegroundColor = "#ff7526",
        DarkForegroundColor = "#ff7526",
        NotRecyclable = true,
        EffectType = EffectType.SOLID_UNDERLINE,
        RiderPresentableNameResourceType = typeof(Strings),
        RiderPresentableNameResourceName = nameof(Strings.InefficientMultiplicationOrder_RiderPresentableName),
        VsGenerateClassificationDefinition = VsGenerateDefinition.VisibleClassification,
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
