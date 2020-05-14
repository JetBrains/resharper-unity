using JetBrains.ReSharper.Feature.Services.Daemon.Attributes;
using JetBrains.ReSharper.Plugins.Unity.Cg.Psi;
using JetBrains.TextControl.DocumentMarkup;

namespace JetBrains.ReSharper.Plugins.Unity.Cg.Daemon
{
    // Highlighters define the look of a highlighting. They belong to a group, which is used to create a settings page
    [RegisterHighlighterGroup(GROUP_ID, "Cg/HLSL",
        HighlighterGroupPriority.LANGUAGE_SETTINGS,
        Language = typeof(CgLanguage),
        // DemoText = DEMO_TEXT,
        RiderNamesProviderType = typeof(CgHighlighterNamesProvider))]

    // Define the highlighters, which describe how a highlighting is displayed
    [RegisterHighlighter(KEYWORD,
        GroupId = GROUP_ID,
        FallbackAttributeId = DefaultLanguageAttributeIds.KEYWORD,
        EffectType = EffectType.TEXT,
        Layer = HighlighterLayer.SYNTAX)]
    [RegisterHighlighter(NUMBER,
        GroupId = GROUP_ID,
        FallbackAttributeId = DefaultLanguageAttributeIds.NUMBER,
        EffectType = EffectType.TEXT,
        Layer = HighlighterLayer.SYNTAX)]
    [RegisterHighlighter(FIELD_IDENTIFIER,
        GroupId = GROUP_ID,
        FallbackAttributeId = DefaultLanguageAttributeIds.FIELD,
        EffectType = EffectType.TEXT,
        Layer = HighlighterLayer.SYNTAX)]
    [RegisterHighlighter(FUNCTION_IDENTIFIER,
        GroupId = GROUP_ID,
        FallbackAttributeId = DefaultLanguageAttributeIds.FUNCTION,
        EffectType = EffectType.TEXT,
        Layer = HighlighterLayer.SYNTAX)]
    [RegisterHighlighter(TYPE_IDENTIFIER,
        GroupId = GROUP_ID,
        FallbackAttributeId = DefaultLanguageAttributeIds.CLASS,
        EffectType = EffectType.TEXT,
        Layer = HighlighterLayer.SYNTAX)]
    [RegisterHighlighter(VARIABLE_IDENTIFIER,
        GroupId = GROUP_ID,
        FallbackAttributeId = DefaultLanguageAttributeIds.LOCAL_VARIABLE,
        EffectType = EffectType.TEXT,
        Layer = HighlighterLayer.SYNTAX)]
    [RegisterHighlighter(LINE_COMMENT,
        GroupId = GROUP_ID,
        FallbackAttributeId = DefaultLanguageAttributeIds.LINE_COMMENT,
        EffectType = EffectType.TEXT,
        Layer = HighlighterLayer.SYNTAX)]
    [RegisterHighlighter(DELIMITED_COMMENT,
        GroupId = GROUP_ID,
        FallbackAttributeId = DefaultLanguageAttributeIds.BLOCK_COMMENT,
        EffectType = EffectType.TEXT,
        Layer = HighlighterLayer.SYNTAX)]
    [RegisterHighlighter(PREPROCESSOR_LINE_CONTENT,
        GroupId = GROUP_ID,
        FallbackAttributeId = DefaultLanguageAttributeIds.CONSTANT,
        EffectType = EffectType.TEXT,
        Layer = HighlighterLayer.SYNTAX)]
    public static class CgHighlightingAttributeIds
    {
        public const string GROUP_ID = "ReSharper Cg Highlighters";

        // All attributes should begin with "ReSharper Cg ". See CgHighlighterNamesProvider below
        public const string KEYWORD = "ReSharper Cg Keyword";
        public const string NUMBER = "ReSharper Cg Number";

        public const string LINE_COMMENT = "ReSharper Cg Line Comment";
        public const string DELIMITED_COMMENT = "ReSharper Cg Delimited Comment";

        public const string FIELD_IDENTIFIER = "ReSharper Cg Field Identifier";
        public const string FUNCTION_IDENTIFIER = "ReSharper Cg Function Identifier";
        public const string TYPE_IDENTIFIER = "ReSharper Cg Type Identifier";
        public const string VARIABLE_IDENTIFIER = "ReSharper Cg Variable Identifier";

        public const string PREPROCESSOR_LINE_CONTENT = "ReSharper Cg Preprocessor Line Content";
    }

    // Convert the ReSharper/Visual Studio friendly IDs into IDEA friendly names. For R# compatibility, all attribute
    // IDs should start with "ReSharper" + some context specific prefix.
    // GetPresentableName will return the attribute ID with attributeIdPrefix or "ReSharper" stripped
    // GetHighlighterTag (for document markup) will return tagPrefix + the uppercase presentable name, with '.' replaced with '_'
    // GetExternalName (for saving) will return "ReSharper." + highlighter tag. For compatibility, don't change this
    public class CgHighlighterNamesProvider : PrefixBasedSettingsNamesProvider
    {
        // Strips "ReSharper Cg" from presentable name, returns "ReSharper.Cg_{UPPERCASE_ATTRIBUTE_ID}"
        // for highlighter tag in markup and uses "ReSharper.ReSharper.Cg_{UPPERCASE_ATTRIBUTE_ID}" to save to
        // IDEA settings.
        public CgHighlighterNamesProvider() : base("ReSharper Cg", "ReSharper.Cg")
        {
        }
    }
}