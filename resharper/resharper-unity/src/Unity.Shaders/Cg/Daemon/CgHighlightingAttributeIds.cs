using JetBrains.ReSharper.Feature.Services.Daemon.Attributes;
using JetBrains.ReSharper.Plugins.Unity.Shaders.Cg.Psi;
using JetBrains.TextControl.DocumentMarkup;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.Cg.Daemon
{
    // Highlighters define the look of a highlighting. They belong to a group, which is used to create a settings page
    [RegisterHighlighterGroup(GROUP_ID, "Cg/HLSL",
        HighlighterGroupPriority.LANGUAGE_SETTINGS,
        Language = typeof(CgLanguage),
        DemoText = DEMO_TEXT,
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
        RiderPresentableName = "Comments//Line comment",
        EffectType = EffectType.TEXT,
        Layer = HighlighterLayer.SYNTAX)]
    [RegisterHighlighter(DELIMITED_COMMENT,
        GroupId = GROUP_ID,
        FallbackAttributeId = DefaultLanguageAttributeIds.BLOCK_COMMENT,
        RiderPresentableName = "Comments//Block comment",
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

        public const string DEMO_TEXT =
@"<ReSharper.Cg_KEYWORD>#pragma</ReSharper.Cg_KEYWORD> <ReSharper.Cg_PREPROCESSOR_LINE_CONTENT>vertex vert</ReSharper.Cg_PREPROCESSOR_LINE_CONTENT>
<ReSharper.Cg_KEYWORD>#pragma</ReSharper.Cg_KEYWORD> <ReSharper.Cg_PREPROCESSOR_LINE_CONTENT>fragment frag</ReSharper.Cg_PREPROCESSOR_LINE_CONTENT>
<ReSharper.Cg_KEYWORD>#include</ReSharper.Cg_KEYWORD> <ReSharper.Cg_PREPROCESSOR_LINE_CONTENT>""UnityCG.cginc""</ReSharper.Cg_PREPROCESSOR_LINE_CONTENT>

<ReSharper.Cg_KEYWORD>struct</ReSharper.Cg_KEYWORD> <ReSharper.Cg_TYPE_IDENTIFIER>v2f</ReSharper.Cg_TYPE_IDENTIFIER>
{
  <ReSharper.Cg_KEYWORD>float2</ReSharper.Cg_KEYWORD> <ReSharper.Cg_VARIABLE_IDENTIFIER>uv</ReSharper.Cg_VARIABLE_IDENTIFIER> : <ReSharper.Cg_KEYWORD>TEXCOORD0</ReSharper.Cg_KEYWORD>;
  <ReSharper.Cg_KEYWORD>float4</ReSharper.Cg_KEYWORD> <ReSharper.Cg_VARIABLE_IDENTIFIER>vertex</ReSharper.Cg_VARIABLE_IDENTIFIER> : <ReSharper.Cg_KEYWORD>SV_POSITION</ReSharper.Cg_KEYWORD>;
};

<ReSharper.Cg_KEYWORD>float</ReSharper.Cg_KEYWORD> <ReSharper.Cg_VARIABLE_IDENTIFIER>_Density</ReSharper.Cg_VARIABLE_IDENTIFIER>;

<ReSharper.Cg_LINE_COMMENT>// Vertex shader</ReSharper.Cg_LINE_COMMENT>
<ReSharper.Cg_TYPE_IDENTIFIER>v2f</ReSharper.Cg_TYPE_IDENTIFIER> <ReSharper.Cg_FUNCTION_IDENTIFIER>vert</ReSharper.Cg_FUNCTION_IDENTIFIER> (<ReSharper.Cg_KEYWORD>float4</ReSharper.Cg_KEYWORD> <ReSharper.Cg_VARIABLE_IDENTIFIER>pos</ReSharper.Cg_VARIABLE_IDENTIFIER> : <ReSharper.Cg_KEYWORD>POSITION</ReSharper.Cg_KEYWORD>, <ReSharper.Cg_KEYWORD>float2</ReSharper.Cg_KEYWORD> <ReSharper.Cg_VARIABLE_IDENTIFIER>uv</ReSharper.Cg_VARIABLE_IDENTIFIER> : <ReSharper.Cg_KEYWORD>TEXCOORD0</ReSharper.Cg_KEYWORD>)
{
  <ReSharper.Cg_TYPE_IDENTIFIER>v2f</ReSharper.Cg_TYPE_IDENTIFIER> <ReSharper.Cg_VARIABLE_IDENTIFIER>o</ReSharper.Cg_VARIABLE_IDENTIFIER>;
  <ReSharper.Cg_VARIABLE_IDENTIFIER>o</ReSharper.Cg_VARIABLE_IDENTIFIER>.<ReSharper.Cg_FIELD_IDENTIFIER>vertex</ReSharper.Cg_FIELD_IDENTIFIER> = <ReSharper.Cg_FUNCTION_IDENTIFIER>UnityObjectToClipPos</ReSharper.Cg_FUNCTION_IDENTIFIER>(<ReSharper.Cg_VARIABLE_IDENTIFIER>pos</ReSharper.Cg_VARIABLE_IDENTIFIER>);
  <ReSharper.Cg_VARIABLE_IDENTIFIER>o</ReSharper.Cg_VARIABLE_IDENTIFIER>.<ReSharper.Cg_FIELD_IDENTIFIER>uv</ReSharper.Cg_FIELD_IDENTIFIER> = <ReSharper.Cg_VARIABLE_IDENTIFIER>uv</ReSharper.Cg_VARIABLE_IDENTIFIER> * <ReSharper.Cg_VARIABLE_IDENTIFIER>_Density</ReSharper.Cg_VARIABLE_IDENTIFIER>;
  <ReSharper.Cg_KEYWORD>return</ReSharper.Cg_KEYWORD> <ReSharper.Cg_VARIABLE_IDENTIFIER>o</ReSharper.Cg_VARIABLE_IDENTIFIER>;
}

<ReSharper.Cg_DELIMITED_COMMENT>/* Fragment shader */</ReSharper.Cg_DELIMITED_COMMENT>
<ReSharper.Cg_TYPE_IDENTIFIER>fixed4</ReSharper.Cg_TYPE_IDENTIFIER> <ReSharper.Cg_FUNCTION_IDENTIFIER>frag</ReSharper.Cg_FUNCTION_IDENTIFIER> (<ReSharper.Cg_TYPE_IDENTIFIER>v2f</ReSharper.Cg_TYPE_IDENTIFIER> <ReSharper.Cg_VARIABLE_IDENTIFIER>i</ReSharper.Cg_VARIABLE_IDENTIFIER>) : <ReSharper.Cg_KEYWORD>SV_Target</ReSharper.Cg_KEYWORD>
{
  <ReSharper.Cg_KEYWORD>float2</ReSharper.Cg_KEYWORD> <ReSharper.Cg_VARIABLE_IDENTIFIER>cd</ReSharper.Cg_VARIABLE_IDENTIFIER>, <ReSharper.Cg_VARIABLE_IDENTIFIER>h</ReSharper.Cg_VARIABLE_IDENTIFIER> = <ReSharper.Cg_VARIABLE_IDENTIFIER>i</ReSharper.Cg_VARIABLE_IDENTIFIER>.<ReSharper.Cg_FIELD_IDENTIFIER>uv</ReSharper.Cg_FIELD_IDENTIFIER>;
  <ReSharper.Cg_VARIABLE_IDENTIFIER>c</ReSharper.Cg_VARIABLE_IDENTIFIER> = <ReSharper.Cg_FUNCTION_IDENTIFIER>floor</ReSharper.Cg_FUNCTION_IDENTIFIER>(<ReSharper.Cg_VARIABLE_IDENTIFIER>c</ReSharper.Cg_VARIABLE_IDENTIFIER>) / <ReSharper.Cg_NUMBER><ReSharper.Cg_NUMBER>2</ReSharper.Cg_NUMBER></ReSharper.Cg_NUMBER>;
  <ReSharper.Cg_KEYWORD>float</ReSharper.Cg_KEYWORD> <ReSharper.Cg_VARIABLE_IDENTIFIER>checker</ReSharper.Cg_VARIABLE_IDENTIFIER> = <ReSharper.Cg_FUNCTION_IDENTIFIER>frac</ReSharper.Cg_FUNCTION_IDENTIFIER>(<ReSharper.Cg_VARIABLE_IDENTIFIER>c</ReSharper.Cg_VARIABLE_IDENTIFIER>.<ReSharper.Cg_FIELD_IDENTIFIER>x</ReSharper.Cg_FIELD_IDENTIFIER> + <ReSharper.Cg_VARIABLE_IDENTIFIER>c</ReSharper.Cg_VARIABLE_IDENTIFIER>.<ReSharper.Cg_FIELD_IDENTIFIER>y</ReSharper.Cg_FIELD_IDENTIFIER>) * <ReSharper.Cg_NUMBER><ReSharper.Cg_NUMBER>2</ReSharper.Cg_NUMBER></ReSharper.Cg_NUMBER>;
  <ReSharper.Cg_KEYWORD>return</ReSharper.Cg_KEYWORD> <ReSharper.Cg_VARIABLE_IDENTIFIER>checker</ReSharper.Cg_VARIABLE_IDENTIFIER>; 
}
";

#region Original code

/*
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"

struct v2f
{
  float2 uv : TEXCOORD0;
  float4 vertex : SV_POSITION;
};

float _Density;

// Vertex shader
v2f vert (float4 pos : POSITION, float2 uv : TEXCOORD0)
{
  v2f o;
  o.vertex = UnityObjectToClipPos(pos);
  o.uv = uv * _Density;
  return o;
}

/* Fragment shader * /
fixed4 frag (v2f i) : SV_Target
{
  float2 c = i.uv;
  c = floor(c) / 2;
  float checker = frac(c.x + c.y) * 2;
  return checker;
}
*/

#endregion
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