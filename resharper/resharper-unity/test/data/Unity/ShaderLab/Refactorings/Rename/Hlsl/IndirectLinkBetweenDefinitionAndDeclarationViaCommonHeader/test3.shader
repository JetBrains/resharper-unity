Shader "ABC"
{
    SubShader
    {
        Pass
        {
            HLSLPROGRAM
            #include "common.hlsl"

            void bar()
            {
            }

            void foo()
            {
                bar();
            }
            ENDHLSL
        }
    }
}