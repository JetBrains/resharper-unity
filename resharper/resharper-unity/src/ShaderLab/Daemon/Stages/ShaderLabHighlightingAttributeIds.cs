using JetBrains.ReSharper.Feature.Services.Daemon.Attributes;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi;
using JetBrains.TextControl.DocumentMarkup;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Daemon.Stages
{
    // Highlighters define the look of a highlighting. They belong to a group, which is used to create a settings page
    [RegisterHighlighterGroup(GROUP_ID, "ShaderLab",
        HighlighterGroupPriority.LANGUAGE_SETTINGS,
        Language = typeof(ShaderLabLanguage),
        // DemoText = DEMO_TEXT,
        RiderNamesProviderType = typeof(ShaderLabHighlighterNamesProvider))]

    // Define the highlighters, which describe how a highlighting is displayed
    [RegisterHighlighter(INJECTED_LANGUAGE_FRAGMENT,
        GroupId = GROUP_ID,
        Layer = HighlighterLayer.SYNTAX)]
    [RegisterHighlighter(NUMBER,
        GroupId = GROUP_ID,
        EffectType = EffectType.TEXT,
        FallbackAttributeId = DefaultLanguageAttributeIds.NUMBER,
        Layer = HighlighterLayer.SYNTAX)]
    [RegisterHighlighter(KEYWORD,
        GroupId = GROUP_ID,
        EffectType = EffectType.TEXT,
        FallbackAttributeId = DefaultLanguageAttributeIds.KEYWORD,
        Layer = HighlighterLayer.SYNTAX)]
    [RegisterHighlighter(STRING,
        GroupId = GROUP_ID,
        EffectType = EffectType.TEXT,
        FallbackAttributeId = DefaultLanguageAttributeIds.STRING,
        Layer = HighlighterLayer.SYNTAX)]
    [RegisterHighlighter(LINE_COMMENT,
        GroupId = GROUP_ID,
        EffectType = EffectType.TEXT,
        RiderPresentableName = "Comments//Line comment",
        FallbackAttributeId = DefaultLanguageAttributeIds.LINE_COMMENT,
        Layer = HighlighterLayer.SYNTAX)]
    [RegisterHighlighter(BLOCK_COMMENT,
        GroupId = GROUP_ID,
        EffectType = EffectType.TEXT,
        RiderPresentableName = "Comments//Block comment",
        FallbackAttributeId = DefaultLanguageAttributeIds.BLOCK_COMMENT,
        Layer = HighlighterLayer.SYNTAX)]
    public static class ShaderLabHighlightingAttributeIds
    {
        public const string GROUP_ID = "ReSharper ShaderLab Highlighters";

        // All attributes should begin with "ReSharper ShaderLab ". See ShaderLabHighlighterNamesProvider below
        public const string INJECTED_LANGUAGE_FRAGMENT = "ReSharper ShaderLab Injected Language Fragment";

        public const string NUMBER = "ReSharper ShaderLab Number";
        public const string KEYWORD = "ReSharper ShaderLab Keyword";
        public const string STRING = "ReSharper ShaderLab String";
        public const string LINE_COMMENT = "ReSharper ShaderLab Line Comment";
        public const string BLOCK_COMMENT = "ReSharper ShaderLab Block Comment";
    }

    // Convert the ReSharper/Visual Studio friendly IDs into IDEA friendly names. For R# compatibility, all attribute
    // IDs should start with "ReSharper" + some context specific prefix.
    // GetPresentableName will return the attribute ID with attributeIdPrefix or "ReSharper" stripped
    // GetHighlighterTag (for document markup) will return tagPrefix + the uppercase presentable name, with '.' replaced with '_'
    // GetExternalName (for saving) will return "ReSharper." + highlighter tag. For compatibility, don't change this
    public class ShaderLabHighlighterNamesProvider : PrefixBasedSettingsNamesProvider
    {
        // Strips "ReSharper ShaderLab" from presentable name, returns "ReSharper.ShaderLab_{UPPERCASE_ATTRIBUTE_ID}"
        // for highlighter tag in markup and uses "ReSharper.ReSharper.ShaderLab_{UPPERCASE_ATTRIBUTE_ID}" to save to
        // IDEA settings.
        public ShaderLabHighlighterNamesProvider()
            : base("ReSharper ShaderLab", "ReSharper.ShaderLab")
        {
        }
    }
}