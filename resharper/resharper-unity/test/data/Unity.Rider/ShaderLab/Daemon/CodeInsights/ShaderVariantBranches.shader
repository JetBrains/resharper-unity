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
            int a;
        #ifdef FOO
            a = a + 1;
        #elif defined(BAR)
            a = a + 2;
        #elif defined(BAZ)
            a = a + 3;
        #endif
        #if defined(A)
            a = a + 4;
        #elif defined(B)
            a = a + 5;
            #ifdef D
            a = a + 9;
            #elif defined(E)
            a = a + 10;
            #elif defined(F)
            a = a + 11;
            #endif
        #elif defined(C) && defined(FOO)
            a = a + 13;
        #else
            a = a + 6;
        #endif
        #ifdef NOT_KEYWORD
            a = a + 7;
        #else
            a = a + 8;
            #ifdef SHADER_API_D3D11
                a = a + 9;
            #else
                a = a + 10;
            #endif
        #endif
        }
        ENDHLSL
    }
}