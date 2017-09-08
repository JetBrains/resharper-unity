Shader "MyShader"
{
    Properties
    {
        [HideInInspector] _Color ("Main Color", Color) = (1,1,1,1)
        _SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
        [HideInInspector] _GlossMap ("Gloss Map", 3D) = "gloss" {}
        [NoScaleOffset] _RefractTex ("Refraction Texture", Cube) = "" {}
        [Enum(UV0,0,UV1,1)] _UVSec ("UV Set for secondary textures", Float) = 0
        [Enum(UV 0,0,UV 1,1,      Trimmed string      , 2)] _UVSec2 ("UV Set 2 for secondary textures", Float) = 0
        [Header(This is an unquoted string with .... and 2345)] _Color2 ("Second Color", Color) = (1,1,1,1)
        [Header(#Value)] _Color3 ("Third Color", Color) = (1,1,1,1)
        [Header(_Value)] _Color4 ("Fourth Color", Color) = (1,1,1,1)
        [Enum(UnityEngine.Rendering.BlendMode)] _Blend ("Blend mode", Float) = 1
        [Enum(One,1,SrcAlpha,5)] _Blend2 ("Blend mode subset", Float) = 1
        [Header(This value() contains parens  ())] _Value ("Some value", Float) = 1
        [Header(Trailing whitespace     )] _Value2("Whatever", Float) = 1
        [Header(    Leading whitespace     )] _Value3("Whatever", Float) = 1

        // Embedded braces
        [Header(Simple mismatched braces))] _Color5 ("Fifth Color", Color) = (1,1,1,1)
        [Header(Simple mismatched braces2((())] _Color6 ("Sixth Color", Color) = (1,1,1,1)
        [Header(Embedded braces))((()))something else)] _Value4 ("Another value", Float) = 1
        [Header(Embedded braces2  ))((()))   something else)] _Value5 ("Another value", Float) = 1
        [Header(Trailing whitepace)         )] _Value6 ("Another value", Float) = 1
        [Header(Trailing whitepace())())()))         )] _Value7 ("Another value", Float) = 1
        [Header(Trailing whitepace2())())())    )         )] _Value8 ("Another value", Float) = 1

        // Unterminated, doesn't display header - unrecognised new line
        [Header(Unterminated] _GlossMap2 ("Gloss Map 2", 3D) = "gloss" {}

        // Invalid characters
        [Header(2 is invalid)] _Color7 ("Seventh Color") = (1,1,1,1)
        [Header("This is not allowed")] _Color8("Eighth Color") = (1,1,1,1)
        [Header(This is wrong!!!!!^$%!)] _Color9("Ninth Color") = (1,1,1,1)

        // Multiple attributes are not an error, but don't work. Need to lex and parse correctly, but show an inspection
        [Header(Mismatched braces (()))))), rubbish rubbish)] _Color5 ("Fifth Color", Color) = (1,1,1,1)
        [Header(More mismatched braces (((()),blah)] _Color5 ("Fifth Color", Color) = (1,1,1,1)
        [NoScaleOffset, Header(Something)] _RefractTex ("Refraction Texture", Cube) = "" {}
        [Header(Argument value), invalid attribute name] _Color6 ("Sixth Color, Color) = (1,1,1,1)
        [Header(Something with spaces, second argument)] _Color3 ("Third Color", Color) = (1,1,1,1)
    }
}
