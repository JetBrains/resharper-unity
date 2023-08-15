Shader "MyShader"
{
    SubShader
    {
        Blend One One
        
        HLSLPROGRAM
        void a{caret}bc()
        {
        }

        void abc2()
        {
        }
        ENDHLSL
    }
}