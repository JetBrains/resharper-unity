Shader "ABC"
{
    SubShader
    {
        Pass
        {
            Name "TestTest"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma target 3.5

            #include "test.cginc"
            #include "test.hlsl"
            #include "reftest3.hlsl"

            struct my_struct
            {
                                                                                                                                                                                                                                                                                 
                                                                                                                                                                                                                                                                             };
            
            void vert(out float4 color: COLOR)
            {
                color = nsvert();
            }

            float4 frag() : COLOR
            {
                ref3();
                return float4(0, 0, 0, 0);
            }
            ENDHLSL
        }
    }
}