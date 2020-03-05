using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Highlightings;
using JetBrains.TextControl.DocumentMarkup;

[assembly:

    // ReSharper doesn't currently support EffectType.LINE_MARKER, so we'll use SOLID_UNDERLINE on the method name
    // instead. Make sure the range is updated in any usages when this is removed!
#if RIDER
    RegisterHighlighter(PerformanceHighlightingAttributeIds.PERFORMANCE_CRITICAL_METHOD_HIGHLIGHTER,
        GroupId = UnityHighlightingGroupIds.Unity,
        BackgroundColor = "#ff7526",
        DarkBackgroundColor = "#ff7526",
        EffectType = EffectType.LINE_MARKER,
        Layer = HighlighterLayer.CARET_ROW - 1,
        TransmitUpdates = true),
#else

    RegisterHighlighter(PerformanceHighlightingAttributeIds.PERFORMANCE_CRITICAL_METHOD_HIGHLIGHTER,
        GroupId = UnityHighlightingGroupIds.Unity,
        EffectColor = "#ff7526",
        EffectType = EffectType.SOLID_UNDERLINE,
        Layer = HighlighterLayer.WARNING + 1),
#endif
    RegisterHighlighter(PerformanceHighlightingAttributeIds.COSTLY_METHOD_INVOCATION,
        GroupId = UnityHighlightingGroupIds.Unity,
        EffectType = EffectType.SOLID_UNDERLINE,
        EffectColor = "#ff7526",
        NotRecyclable = true,
        Layer = HighlighterLayer.SYNTAX),

    RegisterHighlighter(PerformanceHighlightingAttributeIds.NULL_COMPARISON,
        GroupId = UnityHighlightingGroupIds.Unity,
        EffectType = EffectType.SOLID_UNDERLINE,
        EffectColor = "#ff7526",
        NotRecyclable = true,
        Layer = HighlighterLayer.SYNTAX),

    RegisterHighlighter(PerformanceHighlightingAttributeIds.CAMERA_MAIN,
        GroupId = UnityHighlightingGroupIds.Unity,
        EffectColor = "#ff7526",
        NotRecyclable = true,
        EffectType = EffectType.SOLID_UNDERLINE,
        Layer = HighlighterLayer.SYNTAX),
    
    RegisterHighlighter(PerformanceHighlightingAttributeIds.INEFFICIENT_MULTIDIMENSIONAL_ARRAYS_USAGE,
        GroupId = UnityHighlightingGroupIds.Unity,
        EffectColor = "#ff7526",
        NotRecyclable = true,
        EffectType = EffectType.SOLID_UNDERLINE,
        Layer = HighlighterLayer.SYNTAX),
    RegisterHighlighter(PerformanceHighlightingAttributeIds.INEFFICIENT_MULTIPLICATION_ORDER,
        GroupId = UnityHighlightingGroupIds.Unity,
        EffectColor = "#ff7526",
        NotRecyclable = true,
        EffectType = EffectType.SOLID_UNDERLINE,
        Layer = HighlighterLayer.SYNTAX),
]