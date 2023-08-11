Shader "ABC"
{
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            void frag(in float4 pos: {caret}) 
            {
            }
            ENDCG
        }
    }
}
