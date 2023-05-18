Shader "MyShader"
{
    SubShader
    {
        Pass
        {
            CGPROGRAM
                #pragma multi_compile BAR
            
                float3 hsv2rgb(float3 c)
                {
                    return c;
                }
            ENDCG
        }
    }
}