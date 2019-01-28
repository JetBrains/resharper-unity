using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis;
using JetBrains.TextControl.DocumentMarkup;

[assembly:

    // ReSharper doesn't currently support EffectType.LINE_MARKER, so we'll use SOLID_UNDERLINE on the method name
    // instead. Make sure the range is updated in any usages when this is removed!
#if RIDER
    RegisterHighlighter(PerformanceHighlightingAttributeIds.COSTLY_METHOD_HIGHLIGHTER,
        GroupId = UnityHighlightingGroupIds.Unity,
        BackgroundColor = "#ff7526",
        DarkBackgroundColor = "#ff7526",
        EffectType = EffectType.LINE_MARKER,
        Layer = HighlighterLayer.CARET_ROW - 1),
#else
    RegisterHighlighter(PerformanceHighlightingAttributeIds.COSTLY_METHOD_HIGHLIGHTER,
        GroupId = UnityHighlightingGroupIds.Unity,
        BackgroundColor = "#ff7526",
        DarkBackgroundColor = "#ff7526",
        EffectType = EffectType.SOLID_UNDERLINE,
        Layer = HighlighterLayer.CARET_ROW - 1),
#endif

    RegisterHighlighter(PerformanceHighlightingAttributeIds.COSTLY_METHOD_INVOCATION,
        GroupId = UnityHighlightingGroupIds.Unity,
        EffectType = EffectType.SOLID_UNDERLINE,
        EffectColor = "#ff7526",
        Layer = HighlighterLayer.SYNTAX + 1),

    RegisterHighlighter(PerformanceHighlightingAttributeIds.NULL_COMPARISON,
        GroupId = UnityHighlightingGroupIds.Unity,
        EffectType = EffectType.SOLID_UNDERLINE,
        EffectColor = "#ff7526",
        Layer = HighlighterLayer.SYNTAX + 1),

    RegisterHighlighter(PerformanceHighlightingAttributeIds.CAMERA_MAIN,
        GroupId = UnityHighlightingGroupIds.Unity,
        EffectColor = "#ff7526",
        EffectType = EffectType.SOLID_UNDERLINE,
        Layer = HighlighterLayer.SYNTAX + 1),
]