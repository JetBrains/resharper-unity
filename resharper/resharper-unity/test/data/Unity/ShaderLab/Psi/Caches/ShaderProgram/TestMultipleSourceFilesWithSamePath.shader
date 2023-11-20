Shader "FooBar"
{
    SubShader
    {
        HLSLPROGRAM
        #pragma shader_feature FOO BAR BAZ
        #pragma fragment frag

        #include "ShaderVariants.hlsl"
        
        void frag()
        {
        #if BAR
        #endif
        }
        ENDHLSL
    }
}