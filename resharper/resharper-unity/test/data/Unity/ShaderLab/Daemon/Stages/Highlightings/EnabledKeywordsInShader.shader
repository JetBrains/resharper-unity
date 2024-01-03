Shader "FooBar"
{
    SubShader
    {
        HLSLPROGRAM
        #pragma shader_feature FOO BAR BAZ
        #pragma multi_compile A B C
        #pragma multi_compile D E F
        #pragma fragment frag

        #include "ShaderVariants.hlsl"
        
        void frag()
        {
        #if FOO || BAR || BAZ || A || B || C || D || E || F
        #endif

        #if defined(FOO) || defined(BAR) || defined(BAZ) || defined(A) || defined(B) || defined(C) || defined(D) || defined(E) || defined(F)
        #endif

        #ifdef A
        #endif
        #ifdef B
        #endif
        #ifndef C
        #endif
        #ifdef D
        #endif
        }
        ENDHLSL
    }
}