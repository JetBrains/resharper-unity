{caret}HLSLINCLUDE
#pragma foo
ENDHLSL

Shader "Stencil/Stencil Increment No Color"
{
    HLSLINCLUDE

    #include "UnityCG.cginc"

    struct v2f
    {
        float4 position : SV_POSITION;
    };

    v2f vert (appdata_base v)
    {
        v2f o;
        o.position = mul (UNITY_MATRIX_MVP, v.vertex);
        return o;
    }

    fixed4 frag (v2f i) : COLOR
    {
        return fixed4 (1, 1, 1, 1);
    }

    ENDHLSL

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Overlay+1" }
        Pass
        {
            HLSLINCLUDE
            #pragma foo
            ENDHLSL

            ColorMask 0
            Stencil { Pass IncrSat }
            HLSLPROGRAM
            #pragma fragmentoption ARB_precision_hint_fastest
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }
        HLSLINCLUDE
        #pragma foo
        ENDHLSL
    } 
    HLSLINCLUDE
    #pragma foo
    ENDHLSL
}
HLSLINCLUDE
#pragma foo
ENDHLSL
