Shader "Bar"
{
    SubShader
    {
        UsePass "{caret}Foo/BAZ"
    }

    Fallback "Foo"
}
