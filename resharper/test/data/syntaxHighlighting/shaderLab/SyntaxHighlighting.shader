#error This is an error
#warning This is a warning
#line 23
Shader "Unlit/SingleColor"
{
CGINCLUDE
#pragma foo
ENDCG

    /* Block comment. Should have different highlighting to single line comment */
    Properties
    {
        // Color property for material inspector, default to white
        _Color ("Main Color", Color) = (1,1.0,1,1)
    }
    #error Another error
    #warning Another warning
    #line 42
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            // vertex shader
            // this time instead of using "appdata" struct, just spell inputs manually,
            // and instead of returning v2f struct, also just return a single output
            // float4 clip position
            float4 vert (float4 vertex : POSITION) : SV_POSITION
            {
                return mul(UNITY_MATRIX_MVP, vertex);
            }
            
            // color from the material
            fixed4 _Color;

            // pixel shader, no inputs needed
            fixed4 frag () : SV_Target
            {
                return _Color; // just return it
            }
            ENDCG
        }
    }
}
