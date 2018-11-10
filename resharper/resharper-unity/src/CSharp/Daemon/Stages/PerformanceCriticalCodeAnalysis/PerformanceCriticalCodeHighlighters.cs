using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis;
using JetBrains.TextControl.DocumentMarkup;

[assembly:
    RegisterHighlighter(PerformanceHighlightingAttributeIds.COSTLY_METHOD_HIGHLIGHTER,
        GroupId = PerformanceHighlightingAttributeIds.GroupID,
        BackgroundColor = "#ff7526",
        DarkBackgroundColor = "#ff7526",
        EffectType = EffectType.LINE_MARKER,
        Layer = HighlighterLayer.CARET_ROW - 1),
    
    RegisterHighlighter(PerformanceHighlightingAttributeIds.COSTLY_METHOD_INVOCATION,
        GroupId = PerformanceHighlightingAttributeIds.GroupID,
        EffectType = EffectType.SOLID_UNDERLINE,
        EffectColor = "#ff7526",
            
        Layer = HighlighterLayer.SYNTAX + 1),
    RegisterHighlighter(PerformanceHighlightingAttributeIds.NULL_COMPARISON,
        GroupId = PerformanceHighlightingAttributeIds.GroupID,
        EffectType = EffectType.SOLID_UNDERLINE,
        EffectColor = "#ff7526",
        Layer = HighlighterLayer.SYNTAX + 1),

    
    RegisterHighlighter(PerformanceHighlightingAttributeIds.CAMERA_MAIN,
        GroupId = PerformanceHighlightingAttributeIds.GroupID,
        EffectColor = "#ff7526",
        EffectType = EffectType.SOLID_UNDERLINE,
        Layer = HighlighterLayer.SYNTAX + 1),
    
    RegisterHighlighterGroup(PerformanceHighlightingAttributeIds.GroupID, "Unity", HighlighterGroupPriority.LANGUAGE_SETTINGS)
    
]