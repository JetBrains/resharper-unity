using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis;
using JetBrains.TextControl.DocumentMarkup;

[assembly:
    RegisterHighlighter(PerformanceCriticalCodeHighlightingAttributeIds.COSTLY_METHOD_HIGHLIGHTER,
        GroupId = PerformanceCriticalCodeHighlightingAttributeIds.GroupID,
        BackgroundColor = "#ff7526",
        DarkBackgroundColor = "#ff7526",
        EffectType = EffectType.LINE_MARKER,
        Layer = HighlighterLayer.CARET_ROW - 1),
    
    RegisterHighlighter(PerformanceCriticalCodeHighlightingAttributeIds.COSTLY_METHOD_INVOCATION,
        GroupId = PerformanceCriticalCodeHighlightingAttributeIds.GroupID,
        EffectType = EffectType.SOLID_UNDERLINE,
        EffectColor = "#ff7526",
            
        Layer = HighlighterLayer.SYNTAX + 1),
    RegisterHighlighter(PerformanceCriticalCodeHighlightingAttributeIds.NULL_COMPARISON,
        GroupId = PerformanceCriticalCodeHighlightingAttributeIds.GroupID,
        EffectType = EffectType.SOLID_UNDERLINE,
        EffectColor = "#ff7526",
        Layer = HighlighterLayer.SYNTAX + 1),

    
    RegisterHighlighter(PerformanceCriticalCodeHighlightingAttributeIds.CAMERA_MAIN,
        GroupId = PerformanceCriticalCodeHighlightingAttributeIds.GroupID,
        EffectColor = "#ff7526",
        EffectType = EffectType.SOLID_UNDERLINE,
        Layer = HighlighterLayer.SYNTAX + 1),
    
    RegisterHighlighterGroup(PerformanceCriticalCodeHighlightingAttributeIds.GroupID, "Unity", HighlighterGroupPriority.LANGUAGE_SETTINGS)
    
]