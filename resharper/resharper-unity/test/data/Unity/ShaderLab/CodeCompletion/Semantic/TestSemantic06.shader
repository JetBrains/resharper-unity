Shader "ABC"
{
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct v2f
            {
                fixed3 normal: {caret}
            };

            void frag(in v2f input) 
            {
            }
            ENDCG
        }
    }
}
