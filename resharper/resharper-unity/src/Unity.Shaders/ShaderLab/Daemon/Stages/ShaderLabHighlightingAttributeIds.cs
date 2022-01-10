using JetBrains.ReSharper.Feature.Services.Daemon.Attributes;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi;
using JetBrains.TextControl.DocumentMarkup;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Daemon.Stages
{
    // Highlighters define the look of a highlighting. They belong to a group, which is used to create a settings page
    [RegisterHighlighterGroup(GROUP_ID, "ShaderLab",
        HighlighterGroupPriority.LANGUAGE_SETTINGS,
        Language = typeof(ShaderLabLanguage),
        DemoText = DEMO_TEXT,
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

        public const string DEMO_TEXT =
@"<ReSharper.ShaderLab_BLOCK_COMMENT>/* Sample shader */</ReSharper.ShaderLab_BLOCK_COMMENT>
<ReSharper.ShaderLab_KEYWORD>Shader</ReSharper.ShaderLab_KEYWORD> <ReSharper.ShaderLab_STRING>""Custom/SampleShader""</ReSharper.ShaderLab_STRING> {
  <ReSharper.ShaderLab_KEYWORD>Properties</ReSharper.ShaderLab_KEYWORD> {
    _Color (<ReSharper.ShaderLab_STRING>""Color""</ReSharper.ShaderLab_STRING>, <ReSharper.ShaderLab_KEYWORD>Color</ReSharper.ShaderLab_KEYWORD>) = (<ReSharper.ShaderLab_NUMBER>1</ReSharper.ShaderLab_NUMBER>,<ReSharper.ShaderLab_NUMBER>1</ReSharper.ShaderLab_NUMBER>,<ReSharper.ShaderLab_NUMBER>1</ReSharper.ShaderLab_NUMBER>,<ReSharper.ShaderLab_NUMBER>1</ReSharper.ShaderLab_NUMBER>)
    _MainTex (<ReSharper.ShaderLab_STRING>""Albedo (RGB)""</ReSharper.ShaderLab_STRING>, <ReSharper.ShaderLab_KEYWORD>2D</ReSharper.ShaderLab_KEYWORD>) = <ReSharper.ShaderLab_STRING>""white""</ReSharper.ShaderLab_STRING> {}
    _Glossiness (<ReSharper.ShaderLab_STRING>""Smoothness""</ReSharper.ShaderLab_STRING>, <ReSharper.ShaderLab_KEYWORD>Range</ReSharper.ShaderLab_KEYWORD>(<ReSharper.ShaderLab_NUMBER>0</ReSharper.ShaderLab_NUMBER>,<ReSharper.ShaderLab_NUMBER>1</ReSharper.ShaderLab_NUMBER>)) = <ReSharper.ShaderLab_NUMBER>0.5</ReSharper.ShaderLab_NUMBER>
    _Metallic (<ReSharper.ShaderLab_STRING>""Metallic""</ReSharper.ShaderLab_STRING>, <ReSharper.ShaderLab_KEYWORD>Range</ReSharper.ShaderLab_KEYWORD>(<ReSharper.ShaderLab_NUMBER>0</ReSharper.ShaderLab_NUMBER>,<ReSharper.ShaderLab_NUMBER>1</ReSharper.ShaderLab_NUMBER>)) = <ReSharper.ShaderLab_NUMBER>0.0</ReSharper.ShaderLab_NUMBER>
  }
  <ReSharper.ShaderLab_KEYWORD>SubShader</ReSharper.ShaderLab_KEYWORD> {
    <ReSharper.ShaderLab_KEYWORD>Tags</ReSharper.ShaderLab_KEYWORD> { <ReSharper.ShaderLab_STRING>""RenderType""</ReSharper.ShaderLab_STRING>=<ReSharper.ShaderLab_STRING>""Opaque""</ReSharper.ShaderLab_STRING> }
    <ReSharper.ShaderLab_KEYWORD>LOD</ReSharper.ShaderLab_KEYWORD> <ReSharper.ShaderLab_NUMBER>200</ReSharper.ShaderLab_NUMBER>

    <ReSharper.ShaderLab_KEYWORD>CGINCLUDE</ReSharper.ShaderLab_KEYWORD><ReSharper.ShaderLab_INJECTED_LANGUAGE_FRAGMENT>
    <COMMENT>// ...</COMMENT>
    </ReSharper.ShaderLab_INJECTED_LANGUAGE_FRAGMENT><ReSharper.ShaderLab_KEYWORD>ENDCG</ReSharper.ShaderLab_KEYWORD>
  }

  <ReSharper.ShaderLab_LINE_COMMENT>// Use Diffuse as fallback</ReSharper.ShaderLab_LINE_COMMENT>
  <ReSharper.ShaderLab_KEYWORD>FallBack</ReSharper.ShaderLab_KEYWORD> <ReSharper.ShaderLab_STRING>""Diffuse""</ReSharper.ShaderLab_STRING>
}";

#region Original code

/*

/* Sample shader * /
Shader "Custom/SampleShader" {
    Properties {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM

        // ...

        ENDCG
    }

    // Use Diffuse as fallback
    FallBack "Diffuse"
}

 */
#endregion
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