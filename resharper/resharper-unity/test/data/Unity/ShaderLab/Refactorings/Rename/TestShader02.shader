Shader "Bar"
{
    SubShader
    {
        UsePass "Foo/BAZ"
    }

    Fallback "Foo"
}
