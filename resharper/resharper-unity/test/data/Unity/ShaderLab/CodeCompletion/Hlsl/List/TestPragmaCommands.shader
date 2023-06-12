Shader "Test" 
{
    SubShader
    {
        Pass
        {
            HLSLPROGRAM
            #pragma {caret}
            ENDHLSL
        }
    }
}
