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


    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Overlay+1" }
        Pass
        {
            ColorMask 0
            Stencil { Pass IncrSat }
        }
    } 
}
