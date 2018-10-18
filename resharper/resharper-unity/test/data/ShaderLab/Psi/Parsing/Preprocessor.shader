#error This is an error!

#warning This is a warning!

#line 23
{caret}Shader "Unlit/SingleColor"
{
    #error Another error!
    #warning And another warning!
    #line 34 23 23 32 
    Properties
    {
        // Color property for material inspector, default to white
        #ValidName ("Another Color", Color) = (1,1,1,1)
        #warning This is not a valid name!
    }
    SubShader
    {
        // Unity's lexer doesn't require a new line, but it does eat the last character
        #line 23 PPass
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
    // Errors!
    #error
    #warning

    // Valid, means 0
    #line
    }
}
