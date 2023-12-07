Shader "ABC"
{
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
        }

        CGPROGRAM
        #pragma surface surf Lambert
        #pragma multi_compile FOO BAR
        #pragma shader_feature _A _B _C
        #pragma shader_feature_local B C D
        
        struct Input
        {
            #if _{caret} 
            float4 color : COLOR;
        };

        void surf(Input IN, inout SurfaceOutput o)
        {
            o.Albedo = 1;
        }
        ENDCG
    }
}