Shader "FooBar"
{
    SubShader
    {
        HLSLPROGRAM
        #pragma multi_compile A B C
        #pragma fragment frag

        void frag()
        {
        #if SHADER_API_D3D11
        #endif

        #if defined(SHADER_API_METAL)
        #endif

        #ifdef SHADER_API_VULKAN
        #endif

        #if defined(SHADER_API_DESKTOP) || defined(SHADER_API_MOBILE)
        #endif
        }
        ENDHLSL
    }
}