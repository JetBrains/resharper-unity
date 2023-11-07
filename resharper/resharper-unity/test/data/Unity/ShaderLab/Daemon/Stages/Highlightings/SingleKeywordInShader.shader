Shader "FooBar"
{
    SubShader
    {
        HLSLPROGRAM
        #pragma shader_feature FOO
        #pragma shader_feature BAR
        #pragma multi_compile BAZ
        #pragma fragment frag

        #include "ShaderVariants.hlsl"
        
        void frag()
        {
        #if FOO
        #endif
        #if BAR
        #endif
        #if BAZ
        #endif
        }
        ENDHLSL
    }
}