Shader "ABC"
{
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4 frag() : {caret} 
            {
            }
            ENDCG
        }
    }
}
