using System.Drawing;
using JetBrains.ReSharper.Feature.Services.Daemon.Attributes;
using JetBrains.ReSharper.Feature.Services.Daemon.Attributes.Idea;
using JetBrains.ReSharper.Plugins.Unity.Shaders.Resources;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi;
using JetBrains.ReSharper.Psi.Cpp.Daemon;
using JetBrains.TextControl.DocumentMarkup;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Daemon.Stages
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
        Layer = HighlighterLayer.SYNTAX,
        RiderPresentableNameResourceType = typeof(Strings),
        RiderPresentableNameResourceName = nameof(Strings.InjectedLanguageFragment_RiderPresentableName))]
    [RegisterHighlighter(NUMBER,
        GroupId = GROUP_ID,
        EffectType = EffectType.TEXT,
        FallbackAttributeId = DefaultLanguageAttributeIds.NUMBER,
        Layer = HighlighterLayer.SYNTAX,
        RiderPresentableNameResourceType = typeof(Strings),
        RiderPresentableNameResourceName = nameof(Strings.Number_RiderPresentableName))]
    [RegisterHighlighter(KEYWORD,
        GroupId = GROUP_ID,
        EffectType = EffectType.TEXT,
        FallbackAttributeId = DefaultLanguageAttributeIds.KEYWORD,
        Layer = HighlighterLayer.SYNTAX,
        RiderPresentableNameResourceType = typeof(Strings),
        RiderPresentableNameResourceName = nameof(Strings.Keyword_RiderPresentableName))]
    [RegisterHighlighter(STRING,
        GroupId = GROUP_ID,
        EffectType = EffectType.TEXT,
        FallbackAttributeId = DefaultLanguageAttributeIds.STRING,
        Layer = HighlighterLayer.SYNTAX,
        RiderPresentableNameResourceType = typeof(Strings),
        RiderPresentableNameResourceName = nameof(Strings.String_RiderPresentableName))]
    [RegisterHighlighter(LINE_COMMENT,
        GroupId = GROUP_ID,
        EffectType = EffectType.TEXT,
        RiderPresentableNameResourceType = typeof(Strings),
        RiderPresentableNameResourceName = nameof(Strings.CommentsLineComment_RiderPresentableName),
        FallbackAttributeId = DefaultLanguageAttributeIds.LINE_COMMENT,
        Layer = HighlighterLayer.SYNTAX)]
    [RegisterHighlighter(BLOCK_COMMENT,
        GroupId = GROUP_ID,
        EffectType = EffectType.TEXT,
        RiderPresentableNameResourceType = typeof(Strings),
        RiderPresentableNameResourceName = nameof(Strings.CommentsBlockComment_RiderPresentableName),
        FallbackAttributeId = DefaultLanguageAttributeIds.BLOCK_COMMENT,
        Layer = HighlighterLayer.SYNTAX)]
    [RegisterHighlighter(COMMAND,
        GroupId = GROUP_ID,
        EffectType = EffectType.TEXT,
        RiderPresentableNameResourceType = typeof(Strings),
        RiderPresentableNameResourceName = nameof(Strings.Command_RiderPresntableName),
        FallbackAttributeId = KEYWORD,
        Layer = HighlighterLayer.SYNTAX)]
    [RegisterHighlighter(BLOCK_COMMAND,
        GroupId = GROUP_ID,
        EffectType = EffectType.TEXT,
        RiderPresentableNameResourceType = typeof(Strings),
        RiderPresentableNameResourceName = nameof(Strings.BlockCommand_RiderPresntableName),
        FallbackAttributeId = COMMAND,
        Layer = HighlighterLayer.SYNTAX)]
    [RegisterHighlighter(COMMAND_ARGUMENT,
        GroupId = GROUP_ID,
        EffectType = EffectType.TEXT,
        RiderPresentableNameResourceType = typeof(Strings),
        RiderPresentableNameResourceName = nameof(Strings.CommandArgument_RiderPresntableName),
        FallbackAttributeId = KEYWORD,
        Layer = HighlighterLayer.SYNTAX)]
    [RegisterHighlighter(PROPERTY_TYPE,
        GroupId = GROUP_ID,
        EffectType = EffectType.TEXT,
        RiderPresentableNameResourceType = typeof(Strings),
        RiderPresentableNameResourceName = nameof(Strings.PropertyType_RiderPresntableName),
        FallbackAttributeId = KEYWORD,
        Layer = HighlighterLayer.SYNTAX)]
    [RegisterHighlighter(IMPLICITLY_ENABLED_SHADER_KEYWORD,
        GroupId = GROUP_ID,
        EffectType = EffectType.TEXT,
        RiderPresentableNameResourceType = typeof(Strings),
        RiderPresentableNameResourceName = nameof(Strings.ImplicitlyEnabledShaderKeyword_RiderPresentableName),
        FallbackAttributeId = ENABLED_SHADER_KEYWORD,
        Layer = HighlighterLayer.ADDITIONAL_SYNTAX + 1
    )]
    [RegisterHighlighter(ENABLED_SHADER_KEYWORD,
        GroupId = GROUP_ID,
        EffectType = EffectType.TEXT,
        RiderPresentableNameResourceType = typeof(Strings),
        RiderPresentableNameResourceName = nameof(Strings.EnabledShaderKeyword_RiderPresentableName),
        FallbackAttributeId = CppHighlightingAttributeIds.CPP_MACRO_NAME_ATTRIBUTE,
        Layer = HighlighterLayer.ADDITIONAL_SYNTAX + 1
    )]
    [RegisterHighlighter(DISABLED_SHADER_KEYWORD,
        GroupId = GROUP_ID,
        EffectType = EffectType.TEXT,
        RiderPresentableNameResourceType = typeof(Strings),
        RiderPresentableNameResourceName = nameof(Strings.DisabledShaderKeyword_RiderPresentableName),
        FallbackAttributeId = IdeaHighlightingAttributeIds.NOT_USED_ELEMENT_ATTRIBUTES,
        ForegroundColor = "LightGray", 
        DarkForegroundColor = "DarkGray",
        Layer = HighlighterLayer.ADDITIONAL_SYNTAX + 1
    )]
    [RegisterHighlighter(SUPPRESSED_SHADER_KEYWORD,
        GroupId = GROUP_ID,
        EffectType = EffectType.TEXT,
        RiderPresentableNameResourceType = typeof(Strings),
        RiderPresentableNameResourceName = nameof(Strings.SuppressedShaderKeyword_RiderPresentableName),
        FontStyle = FontStyle.Strikeout,
        Layer = HighlighterLayer.ADDITIONAL_SYNTAX + 1
    )]
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
        public const string IMPLICITLY_ENABLED_SHADER_KEYWORD = "ReSharper ShaderLab Implicitly Enabled Shader Keyword";
        public const string ENABLED_SHADER_KEYWORD = "ReSharper ShaderLab Enabled Shader Keyword";
        public const string SUPPRESSED_SHADER_KEYWORD = "ReSharper ShaderLab Suppressed Shader Keyword";
        public const string DISABLED_SHADER_KEYWORD = "ReSharper ShaderLab Disabled Shader Keyword";
        public const string COMMAND = "ReSharper ShaderLab Command";
        public const string BLOCK_COMMAND = "ReSharper ShaderLab Block Command";
        public const string PROPERTY_TYPE = "ReSharper ShaderLab Property Type";
        public const string COMMAND_ARGUMENT = "ReSharper ShaderLab Command Argument";

        public const string DEMO_TEXT =
@"<ReSharper.ShaderLab_BLOCK_COMMENT>/* Sample shader */</ReSharper.ShaderLab_BLOCK_COMMENT>
<ReSharper.ShaderLab_BLOCK_COMMAND>Shader</ReSharper.ShaderLab_BLOCK_COMMAND> <ReSharper.ShaderLab_STRING>""Custom/SampleShader""</ReSharper.ShaderLab_STRING> {
  <ReSharper.ShaderLab_BLOCK_COMMAND>Properties</ReSharper.ShaderLab_BLOCK_COMMAND> {
    _Color (<ReSharper.ShaderLab_STRING>""Color""</ReSharper.ShaderLab_STRING>, <ReSharper.ShaderLab_PROPERTY_TYPE>Color</ReSharper.ShaderLab_PROPERTY_TYPE>) = (<ReSharper.ShaderLab_NUMBER>1</ReSharper.ShaderLab_NUMBER>,<ReSharper.ShaderLab_NUMBER>1</ReSharper.ShaderLab_NUMBER>,<ReSharper.ShaderLab_NUMBER>1</ReSharper.ShaderLab_NUMBER>,<ReSharper.ShaderLab_NUMBER>1</ReSharper.ShaderLab_NUMBER>)
    _MainTex (<ReSharper.ShaderLab_STRING>""Albedo (RGB)""</ReSharper.ShaderLab_STRING>, <ReSharper.ShaderLab_PROPERTY_TYPE>2D</ReSharper.ShaderLab_PROPERTY_TYPE>) = <ReSharper.ShaderLab_STRING>""white""</ReSharper.ShaderLab_STRING> {}
    _Glossiness (<ReSharper.ShaderLab_STRING>""Smoothness""</ReSharper.ShaderLab_STRING>, <ReSharper.ShaderLab_PROPERTY_TYPE>Range</ReSharper.ShaderLab_PROPERTY_TYPE>(<ReSharper.ShaderLab_NUMBER>0</ReSharper.ShaderLab_NUMBER>,<ReSharper.ShaderLab_NUMBER>1</ReSharper.ShaderLab_NUMBER>)) = <ReSharper.ShaderLab_NUMBER>0.5</ReSharper.ShaderLab_NUMBER>
    _Metallic (<ReSharper.ShaderLab_STRING>""Metallic""</ReSharper.ShaderLab_STRING>, <ReSharper.ShaderLab_PROPERTY_TYPE>Range</ReSharper.ShaderLab_PROPERTY_TYPE>(<ReSharper.ShaderLab_NUMBER>0</ReSharper.ShaderLab_NUMBER>,<ReSharper.ShaderLab_NUMBER>1</ReSharper.ShaderLab_NUMBER>)) = <ReSharper.ShaderLab_NUMBER>0.0</ReSharper.ShaderLab_NUMBER>
  }
  <ReSharper.ShaderLab_BLOCK_COMMAND>SubShader</ReSharper.ShaderLab_BLOCK_COMMAND> {
    <ReSharper.ShaderLab_BLOCK_COMMAND>Tags</ReSharper.ShaderLab_BLOCK_COMMAND> { <ReSharper.ShaderLab_STRING>""RenderType""</ReSharper.ShaderLab_STRING>=<ReSharper.ShaderLab_STRING>""Opaque""</ReSharper.ShaderLab_STRING> }
    <ReSharper.ShaderLab_COMMAND>LOD</ReSharper.ShaderLab_COMMAND> <ReSharper.ShaderLab_NUMBER>200</ReSharper.ShaderLab_NUMBER>
    <ReSharper.ShaderLab_COMMAND>Cull</ReSharper.ShaderLab_COMMAND> <ReSharper.ShaderLab_COMMAND_ARGUMENT>Front</ReSharper.ShaderLab_COMMAND_ARGUMENT>

    <ReSharper.ShaderLab_KEYWORD>CGINCLUDE</ReSharper.ShaderLab_KEYWORD><ReSharper.ShaderLab_INJECTED_LANGUAGE_FRAGMENT>
    <COMMENT>// ...</COMMENT>
    <CPP_DIRECTIVE>#pragma</CPP_DIRECTIVE> shader_feature <ReSharper.ShaderLab_SUPPRESSED_SHADER_KEYWORD>RED</ReSharper.ShaderLab_SUPPRESSED_SHADER_KEYWORD> <ReSharper.ShaderLab_ENABLED_SHADER_KEYWORD>GREEN</ReSharper.ShaderLab_ENABLED_SHADER_KEYWORD> <ReSharper.ShaderLab_DISABLED_SHADER_KEYWORD>BLUE</ReSharper.ShaderLab_DISABLED_SHADER_KEYWORD>
    <CPP_DIRECTIVE>#pragma</CPP_DIRECTIVE> multi_compile <ReSharper.ShaderLab_IMPLICITLY_ENABLED_SHADER_KEYWORD>HALF</ReSharper.ShaderLab_IMPLICITLY_ENABLED_SHADER_KEYWORD> <ReSharper.ShaderLab_DISABLED_SHADER_KEYWORD>FULL</ReSharper.ShaderLab_DISABLED_SHADER_KEYWORD>
    
    <CPP_TYPEDEF_IDENTIFIER>float3</CPP_TYPEDEF_IDENTIFIER> <CPP_GLOBAL_FUNCTION_IDENTIFIER>get_color</CPP_GLOBAL_FUNCTION_IDENTIFIER><CPP_PARENTHESES>()</CPP_PARENTHESES> <CPP_BRACES>{</CPP_BRACES>
      <CPP_TYPEDEF_IDENTIFIER>float3</CPP_TYPEDEF_IDENTIFIER> <CPP_LOCAL_VARIABLE_IDENTIFIER>c</CPP_LOCAL_VARIABLE_IDENTIFIER>;
<CPP_DIRECTIVE>#if</CPP_DIRECTIVE> defined<CPP_PARENTHESES>(</CPP_PARENTHESES><ReSharper.ShaderLab_SUPPRESSED_SHADER_KEYWORD>RED</ReSharper.ShaderLab_SUPPRESSED_SHADER_KEYWORD><CPP_PARENTHESES>)</CPP_PARENTHESES>
      <CPP_PREPROCESSOR_INACTIVE_BRANCH>c = float3(1, 0, 0);</CPP_PREPROCESSOR_INACTIVE_BRANCH>
<CPP_DIRECTIVE>#elif</CPP_DIRECTIVE> defined<CPP_PARENTHESES>(</CPP_PARENTHESES><ReSharper.ShaderLab_ENABLED_SHADER_KEYWORD>GREEN</ReSharper.ShaderLab_ENABLED_SHADER_KEYWORD><CPP_PARENTHESES>)</CPP_PARENTHESES>
      <CPP_LOCAL_VARIABLE_IDENTIFIER>c</CPP_LOCAL_VARIABLE_IDENTIFIER> = <CPP_TYPEDEF_IDENTIFIER>float3</CPP_TYPEDEF_IDENTIFIER>(<CPP_FLOAT_LITERAL>0</CPP_FLOAT_LITERAL>, <CPP_FLOAT_LITERAL>1</CPP_FLOAT_LITERAL>, <CPP_FLOAT_LITERAL>0</CPP_FLOAT_LITERAL>);
<CPP_DIRECTIVE>#elif</CPP_DIRECTIVE> defined<CPP_PARENTHESES>(</CPP_PARENTHESES><ReSharper.ShaderLab_DISABLED_SHADER_KEYWORD>BLUE</ReSharper.ShaderLab_DISABLED_SHADER_KEYWORD><CPP_PARENTHESES>)</CPP_PARENTHESES>
      <CPP_PREPROCESSOR_INACTIVE_BRANCH>c = float3<CPP_PARENTHESES>(</CPP_PARENTHESES>0, 0, 1<CPP_PARENTHESES>)</CPP_PARENTHESES>;</CPP_PREPROCESSOR_INACTIVE_BRANCH>
<CPP_DIRECTIVE>#endif</CPP_DIRECTIVE>
<CPP_DIRECTIVE>#ifdef</CPP_DIRECTIVE> <ReSharper.ShaderLab_IMPLICITLY_ENABLED_SHADER_KEYWORD>HALF</ReSharper.ShaderLab_IMPLICITLY_ENABLED_SHADER_KEYWORD>
      <CPP_LOCAL_VARIABLE_IDENTIFIER>c</CPP_LOCAL_VARIABLE_IDENTIFIER> = <CPP_LOCAL_VARIABLE_IDENTIFIER>c</CPP_LOCAL_VARIABLE_IDENTIFIER> * <CPP_FLOAT_LITERAL>0.5</CPP_FLOAT_LITERAL>;
<CPP_DIRECTIVE>#endif</CPP_DIRECTIVE>
      <CPP_KEYWORD>return</CPP_KEYWORD> <CPP_LOCAL_VARIABLE_IDENTIFIER>c</CPP_LOCAL_VARIABLE_IDENTIFIER>;
    <CPP_BRACES>}</CPP_BRACES>
    </ReSharper.ShaderLab_INJECTED_LANGUAGE_FRAGMENT><ReSharper.ShaderLab_KEYWORD>ENDCG</ReSharper.ShaderLab_KEYWORD>
  }

  <ReSharper.ShaderLab_LINE_COMMENT>// Use Diffuse as fallback</ReSharper.ShaderLab_LINE_COMMENT>
  <ReSharper.ShaderLab_COMMAND>FallBack</ReSharper.ShaderLab_COMMAND> <ReSharper.ShaderLab_STRING>""Diffuse""</ReSharper.ShaderLab_STRING>
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
