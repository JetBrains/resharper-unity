Shader "Bar"
{
    SubShader
    {
        UsePass "Foo/B{caret}AZ"
    }

    Fallback "Foo"
}
