Shader "ABC"
{
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            void frag(in float2 tex: {caret}) 
            {
            }
            ENDCG
        }
    }
}
