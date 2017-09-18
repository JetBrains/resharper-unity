using JetBrains.ReSharper.Feature.Services.Daemon.IdeaAttributes;
using JetBrains.ReSharper.Plugins.Unity.Cg.Daemon;
using JetBrains.TextControl.DocumentMarkup;

[assembly:
    RegisterHighlighter(CgHighlightingAttributeIds.KEYWORD,
#if RIDER
        GroupId = CgHighligherGroup.ID,
#endif
        FallbackAttributeId = IdeaHighlightingAttributeIds.KEYWORD, DarkForegroundColor = "#569CD6", EffectType = EffectType.TEXT, ForegroundColor = "#0000E0", Layer = HighlighterLayer.SYNTAX),
    
    RegisterHighlighter(CgHighlightingAttributeIds.NUMBER,
#if RIDER
        GroupId = CgHighligherGroup.ID,
#endif
        FallbackAttributeId = IdeaHighlightingAttributeIds.NUMBER,
        DarkForegroundColor = "#B5CEA8", EffectType = EffectType.TEXT, ForegroundColor = "#000000", Layer = HighlighterLayer.SYNTAX),
    
    RegisterHighlighter(CgHighlightingAttributeIds.FIELD_IDENTIFIER, 
#if RIDER
        GroupId = CgHighligherGroup.ID,
#endif
        FallbackAttributeId = IdeaHighlightingAttributeIds.INSTANCE_FIELD, DarkForegroundColor = "Violet", EffectType = EffectType.TEXT, ForegroundColor = "Purple", Layer = HighlighterLayer.SYNTAX),
    
    RegisterHighlighter(CgHighlightingAttributeIds.METHOD_IDENTIFIER, 
#if RIDER
        GroupId = CgHighligherGroup.ID,
#endif
        FallbackAttributeId = IdeaHighlightingAttributeIds.INSTANCE_METHOD, DarkForegroundColor = "Cyan", EffectType = EffectType.TEXT, ForegroundColor = "DarkCyan:Maroon", Layer = HighlighterLayer.SYNTAX),
    
    RegisterHighlighter(CgHighlightingAttributeIds.TYPE_IDENTIFIER,
#if RIDER
        GroupId = CgHighligherGroup.ID,
#endif
        FallbackAttributeId = IdeaHighlightingAttributeIds.CLASS_NAME, DarkForegroundColor = "LightBlue", EffectType = EffectType.TEXT, ForegroundColor = "DarkBlue", Layer = HighlighterLayer.SYNTAX),
    
    RegisterHighlighter(CgHighlightingAttributeIds.VARIABLE_IDENTIFIER,
#if RIDER
        GroupId = CgHighligherGroup.ID,
#endif
        FallbackAttributeId = IdeaHighlightingAttributeIds.LOCAL_VARIABLE, EffectType = EffectType.TEXT, Layer = HighlighterLayer.SYNTAX),
    
    RegisterHighlighter(CgHighlightingAttributeIds.LINE_COMMENT,
#if RIDER
        GroupId = CgHighligherGroup.ID,
#endif
        FallbackAttributeId = IdeaHighlightingAttributeIds.LINE_COMMENT, DarkForegroundColor = "#007F00", EffectType = EffectType.TEXT, ForegroundColor = "#57A64A", Layer = HighlighterLayer.SYNTAX),
    
    RegisterHighlighter(CgHighlightingAttributeIds.DELIMETED_COMMENT,
#if RIDER
        GroupId = CgHighligherGroup.ID,
#endif
        FallbackAttributeId = IdeaHighlightingAttributeIds.BLOCK_COMMENT, DarkForegroundColor = "#007F00", EffectType = EffectType.TEXT, ForegroundColor = "#57A64A", Layer = HighlighterLayer.SYNTAX),
    
    RegisterHighlighter(CgHighlightingAttributeIds.PREPPROCESSOR_LINE_CONTENT,
#if RIDER
        GroupId = CgHighligherGroup.ID,
#endif
        FallbackAttributeId = IdeaHighlightingAttributeIds.CONSTANT, DarkForegroundColor = "Violet", EffectType = EffectType.TEXT, ForegroundColor = "Purple", Layer = HighlighterLayer.SYNTAX)
]


namespace JetBrains.ReSharper.Plugins.Unity.Cg.Daemon
{
    public static class CgHighlightingAttributeIds
    {
        public const string KEYWORD = "ReSharper Cg Keyword";
        public const string NUMBER = "ReSharper Cg Number";

        public const string LINE_COMMENT = "ReSharper Cg Line Comment";
        public const string DELIMETED_COMMENT = "ReSharper Cg Delimeted Comment";

        public const string FIELD_IDENTIFIER = "ReSharper Cg Field Identifier";
        public const string METHOD_IDENTIFIER = "ReSharper Cg Method Identifier";
        public const string TYPE_IDENTIFIER = "ReSharper Cg Type Identifier";
        public const string VARIABLE_IDENTIFIER = "ReSharper Cg Variable Identifier";

        public const string PREPPROCESSOR_LINE_CONTENT = "ReSharper Cg Preprocessor Line Content";
    }
}