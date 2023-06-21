Shader "Foo" {
    HLSLINCLUDE
    #include "Unused.hlsl"
    #include "Used.hlsl"
    ENDHLSL
    
    SubShader
    {
        Pass
        {
            HLSLPROGRAM
            #pragma fragment frag

            #include "Unused.hlsl"
            #include "Used.hlsl"

            void frag()
            {
                usedFn();
            }
            ENDHLSL
        }
    }
}
