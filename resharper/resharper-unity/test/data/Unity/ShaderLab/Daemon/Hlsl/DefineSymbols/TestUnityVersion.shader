Shader "Custom/TestUnityVersion"
{
    Properties
    {
        _Color ("Main Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        
        Pass
        {
            HLSLPROGRAM
            
            #ifndef TEST_UNITY_VERSION_INCLUDED
            #define TEST_UNITY_VERSION_INCLUDED
            
            float4 TestUnityVersionFunc() {
                #if UNITY_VERSION >= 202100
                    return float4(0, 1, 0, 1);
                #elif UNITY_VERSION >= 20200
                    return float4(1, 1, 0, 1);
                #else
                    return float4(1, 1, 1, 1);
                #endif
            }
            
            #endif
            
            ENDHLSL
        }
    }
}