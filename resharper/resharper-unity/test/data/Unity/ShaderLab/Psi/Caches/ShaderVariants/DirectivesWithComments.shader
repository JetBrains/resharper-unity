Shader "MyShader"
{
    HLSLINCLUDE
    #pragma multi_compile _ K1 /*Comment*/ K2
    #pragma multi_compile _ K3/*Comment*/K4
    #pragma shader_feature K5//K6
    #pragma shader_feature K7 // K8
    #pragma shader_feature /*K9*/ K10
    #pragma shader_feature /*
    Comments here
    */ K11
    #pragma shader_feature K12 \
        K13 \
        K\
14
    #pragma multi_compile K15 K16*/
    ENDHLSL
}