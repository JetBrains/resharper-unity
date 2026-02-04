Shader "Custom/TestUnityVersion"
{
    SubShader
    {
        Pass
        {
            HLSLPROGRAM

            #include "TestUnityVersion1.hlsl"
            
            float4 vert() : SV_POSITION
            {
                return TestUnityVersionFunc();
            }
            
            ENDHLSL
        }
    }
}