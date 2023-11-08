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
        #pragma shader_feature A B C
        #pragma shader_feature_local B C D
        
        struct Input
        {
            #if {caret} 
            float4 color : COLOR;
        };

        void surf(Input IN, inout SurfaceOutput o)
        {
            o.Albedo = 1;
        }
        ENDCG
    }
}