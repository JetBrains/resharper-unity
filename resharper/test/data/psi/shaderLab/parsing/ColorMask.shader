{caret}Shader "Unlit/SingleColor"
{
    SubShader
    {
        Pass { ColorMask A }
        Pass { ColorMask RGB }
        Pass { ColorMask AGBR }
        Pass { ColorMask 0 }
    }
}
