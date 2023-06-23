Shader "Bar"
{
    SubShader
    {
        UsePass "Foo/BAZ"
    }

    Fallback "{caret}Foo"
}
