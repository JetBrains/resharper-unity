Shader "ABC"
{
    SubShader
    {
        Pass
        {
            CGPROGRAM
            fix{caret}ed4 v;

            struct abc {
                void bar() { }
            }
            ENDCG
        }
    }
}
