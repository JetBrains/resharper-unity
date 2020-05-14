using System.Drawing;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.TextControl.DocumentMarkup;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings
{
    // Highlighters define the look of a highlighting. They belong to a group, which is used to create a settings page
    [RegisterHighlighterGroup(GROUP_ID, "Unity",
        HighlighterGroupPriority.LANGUAGE_SETTINGS,
        Language = typeof(CSharpLanguage),
        // DemoText = DemoText,
        RiderNamesProviderType = typeof(UnityHighlighterNamesProvider))]

    // Define the highlighters, which describe how a highlighting is displayed
    [RegisterHighlighter(UNITY_GUTTER_ICON_ATTRIBUTE,
        EffectType = EffectType.GUTTER_MARK,
        GutterMarkType = typeof(UnityGutterMark),
        Layer = HighlighterLayer.SYNTAX + 1)]
    [RegisterHighlighter(UNITY_PERFORMANCE_CRITICAL_GUTTER_ICON_ATTRIBUTE,
        EffectType = EffectType.GUTTER_MARK,
        GutterMarkType = typeof(UnityHotGutterMark),
        Layer = HighlighterLayer.SYNTAX + 1)]
    [RegisterHighlighter(UNITY_IMPLICITLY_USED_IDENTIFIER_ATTRIBUTE,
        GroupId = UnityHighlightingGroupIds.Unity,
        EffectType = EffectType.TEXT,
        FontStyle = FontStyle.Bold,
        Layer = HighlighterLayer.SYNTAX + 1)]
    public static class UnityHighlightingAttributeIds
    {
        public const string GROUP_ID = "Unity";

        // Not text based highlighters, so don't appear in the settings page
        public const string UNITY_GUTTER_ICON_ATTRIBUTE = "Unity Gutter Icon";
        public const string UNITY_PERFORMANCE_CRITICAL_GUTTER_ICON_ATTRIBUTE = "Unity Performance Critical Icon Gutter Icon";

        // All attributes should begin with "ReSharper Cg ". See CgHighlighterNamesProvider below
        public const string UNITY_IMPLICITLY_USED_IDENTIFIER_ATTRIBUTE = "ReSharper Unity Implicitly Used Identifier";
    }

    // Convert the ReSharper/Visual Studio friendly IDs into IDEA friendly names. For R# compatibility, all attribute
    // IDs should start with "ReSharper" + some context specific prefix.
    // GetPresentableName will return the attribute ID with attributeIdPrefix or "ReSharper" stripped
    // GetHighlighterTag (for document markup) will return tagPrefix + the uppercase presentable name, with '.' replaced with '_'
    // GetExternalName (for saving) will return "ReSharper." + highlighter tag. For compatibility, don't change this
    public class UnityHighlighterNamesProvider : PrefixBasedSettingsNamesProvider
    {
        // Strips "ReSharper Unity" from presentable name, uses "UNITY_{UPPERCASE_ATTRIBUTE_ID}" for document markup and
        // saving IDEA settings.
        public UnityHighlighterNamesProvider()
            : base("ReSharper Unity", "UNITY")
        {
        }

        // Return "UNITY_{UPPERCASE_ATTRIBUTE_ID}" instead of "ReSharper.UNITY_{UPPERCASE_ATTRIBUTE_ID}". We don't need
        // the "ReSharper." prefix as UNITY will not clash with anything in IDEA
        public override string GetExternalName(string attributeId) => GetHighlighterTag(attributeId);
    }
}
