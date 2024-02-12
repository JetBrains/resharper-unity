Shader "ABC"
{
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature_local_vertex _SHAPE_A _SHAPE_B _SHAPE_C
            #pragma multi_compile_local_fragment _SHAPE_D _SHAPE_E _SHAPE_F
            
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {                        
                float4 vertex : SV_POSITION;
            };
           
            v2f vert (appdata v)
            {
                #if _SHAPE{caret}
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);            
                return o;
            }
            
            half4 frag (v2f i) : SV_Target
            {            
                return 1;
            }
            ENDCG
        }
    }
}