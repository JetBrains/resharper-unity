/*
$$ CheckShaderApi: Metal
*/
Shader "FooBar"
{
    SubShader
    {
        HLSLPROGRAM
        #pragma multi_compile A B C
        #pragma fragment frag

        #include "ShaderVariants.hlsl"
        
        void frag()
        {
        #if SHADER_API_D3D11
        #endif

        #if defined(SHA{caret}DER_API_METAL)
        #endif

        #ifdef SHADER_API_VULKAN
        #endif

        #if A || B || C
        #endif
        }
        ENDHLSL
    }
}