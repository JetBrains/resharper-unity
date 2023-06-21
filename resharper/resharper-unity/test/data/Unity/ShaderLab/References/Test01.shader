Shader "ABC"
{
    SubShader
    {
        Pass
        {
CGPROGRAM
#define VEC(X)           \
    X ## 1 my ## X ## 1; \
    X ## 2 my ## X ## 2; \
    X ## 3 my ## X ## 3; \
    X ## 4 my ## X ## 4;

#define MATRIX(X)           \
    X ## 1x1 my ## X ## 11; \
    X ## 1x2 my ## X ## 12; \
    X ## 1x3 my ## X ## 13; \
    X ## 1x4 my ## X ## 14; \
                            \
    X ## 2x1 my ## X ## 21; \
    X ## 2x2 my ## X ## 22; \
    X ## 2x3 my ## X ## 23; \
    X ## 2x4 my ## X ## 24; \
                            \
    X ## 3x1 my ## X ## 31; \
    X ## 3x2 my ## X ## 32; \
    X ## 3x3 my ## X ## 33; \
    X ## 3x4 my ## X ## 34; \
                            \
    X ## 4x1 my ## X ## 41; \
    X ## 4x2 my ## X ## 42; \
    X ## 4x3 my ## X ## 43; \
    X ## 4x4 my ## X ## 44;

VEC(fixed)
MATRIX(fixed)
ENDCG
        }
    }
}
