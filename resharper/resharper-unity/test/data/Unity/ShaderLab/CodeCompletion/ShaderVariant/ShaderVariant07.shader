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
        #pragma shader_feature _SHAPE_A _SHAPE_B _SHAPE_C
        
        struct Input
        {
            #if _SHAPE{caret} 
            float4 color : COLOR;
        };

        void surf(Input IN, inout SurfaceOutput o)
        {
            o.Albedo = 1;
        }
        ENDCG
    }
}