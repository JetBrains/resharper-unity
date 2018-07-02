using JetBrains.ReSharper.Feature.Services.Daemon.IdeaAttributes;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Daemon.Stages;
using JetBrains.TextControl.DocumentMarkup;

[assembly:
    RegisterHighlighterGroup(ShaderLabHighlightingAttributeIds.GroupID, "ShaderLab", HighlighterGroupPriority.LANGUAGE_SETTINGS,
        RiderNamesProviderType = typeof(ShaderLabHighlighterNamesProvider)),
    
    RegisterHighlighter(ShaderLabHighlightingAttributeIds.INJECTED_LANGUAGE_FRAGMENT,
        GroupId = ShaderLabHighlightingAttributeIds.GroupID,  
        Layer = HighlighterLayer.SYNTAX),
    
    RegisterHighlighter(ShaderLabHighlightingAttributeIds.NUMBER,
        GroupId = ShaderLabHighlightingAttributeIds.GroupID,
        EffectType = EffectType.TEXT,
        FallbackAttributeId = IdeaHighlightingAttributeIds.NUMBER,
        Layer = HighlighterLayer.SYNTAX),
    
    RegisterHighlighter(ShaderLabHighlightingAttributeIds.KEYWORD,
        GroupId = ShaderLabHighlightingAttributeIds.GroupID,
        EffectType = EffectType.TEXT,
        FallbackAttributeId = IdeaHighlightingAttributeIds.KEYWORD,
        Layer = HighlighterLayer.SYNTAX),
    
    RegisterHighlighter(ShaderLabHighlightingAttributeIds.STRING,
        GroupId = ShaderLabHighlightingAttributeIds.GroupID,
        EffectType = EffectType.TEXT,
        FallbackAttributeId = IdeaHighlightingAttributeIds.STRING,
        Layer = HighlighterLayer.SYNTAX),
    
    RegisterHighlighter(ShaderLabHighlightingAttributeIds.LINE_COMMENT,
        GroupId = ShaderLabHighlightingAttributeIds.GroupID,
        EffectType = EffectType.TEXT,
        FallbackAttributeId = IdeaHighlightingAttributeIds.LINE_COMMENT,
        Layer = HighlighterLayer.SYNTAX),
    
    RegisterHighlighter(ShaderLabHighlightingAttributeIds.BLOCK_COMMENT,
        GroupId = ShaderLabHighlightingAttributeIds.GroupID,
        EffectType = EffectType.TEXT,
        FallbackAttributeId = IdeaHighlightingAttributeIds.BLOCK_COMMENT,
        Layer = HighlighterLayer.SYNTAX)
]

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Daemon.Stages
{
    public static class ShaderLabHighlightingAttributeIds
    {
        public const string GroupID = "ReSharper ShaderLab Highlighters";
        
        public const string INJECTED_LANGUAGE_FRAGMENT = "ReSharper ShaderLab Injected Language Fragment";
        
        public const string NUMBER = "ReSharper ShaderLab Number";
        public const string KEYWORD = "ReSharper ShaderLab Keyword";        
        public const string STRING = "ReSharper ShaderLab String";        
        public const string LINE_COMMENT = "ReSharper ShaderLab Line Comment";        
        public const string BLOCK_COMMENT = "ReSharper ShaderLab Block Comment";
    }

    public class ShaderLabHighlighterNamesProvider : PrefixBasedSettingsNamesProvider
    {
        public ShaderLabHighlighterNamesProvider()
            : base("ReSharper ShaderLab", "ReSharper.ShaderLab")
        {
        }
    }    
}