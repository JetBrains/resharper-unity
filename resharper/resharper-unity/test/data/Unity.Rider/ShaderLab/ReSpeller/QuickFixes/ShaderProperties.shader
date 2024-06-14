Shader "Custom/Unlit" {
    Properties {
        _Color ("Colg{caret}or", Color) = (1,1,1,1)
    }
    SubShader {
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            fixed4 _Color;
            struct v2f { float4 pos : SV_POSITION; };
            v2f vert(float4 v : POSITION)
            {
                return v2f(UnityObjectToClipPos(v));
            }
            fixed4 frag(v2f i) : SV_Target {
                return _Color;
            }
            ENDCG
        }
    }
}
