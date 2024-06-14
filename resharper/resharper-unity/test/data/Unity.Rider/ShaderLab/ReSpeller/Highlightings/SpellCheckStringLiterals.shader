Shader "TestCases/SpiellCheck"
{
    // SpiellCheck shader
    Properties
    {
        _MainTextue ("Main Texture (RGB)", 2D) = "whitte" {} // Typo: should be _MainTex
        _Color ("Tint Color", Color) = (1,1,1,1)
        _Shiney ("Shinins", Range(0.03, 1)) = 0.078125 // Typo: should be _Shininess
    }

    SubShader
    {
        Tags { "RenderType" = "Opafque" }
        LOD 200

        CGPROGRAM
#pragma surface surf Standard fullforwardshadows

        struct Input
        {
            float2 uv_MainTextue; // Typo: should be uv_MainTex
        };

        sampler2D _MainTextue; // Typo: should be _MainTex
        half4 _Color;
        half _Shiney; // Typo: should be _Shininess

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            half4 c = tex2D(_MainTextue, IN.uv_MainTextue) * _Color; // Typos
            o.Albedo = c.rgb;
            o.Alpha = c.a;
            o.Metallic = _Shiney; // Typo
        }
        ENDCG
    }
// incorrect phrase part should be detected
    FallBack "Difuse"
}