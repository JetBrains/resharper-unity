{caret}Shader "Blah"
{
    SubShader
    {
        Pass
        {
            Cull Front
            ZWrite On
            ZTest Less
            Offset 0, -1
        }
        Pass
        {
            Cull Back
            ZTest LEqual
            Offset -1, -1
            ZWrite Off
        }
        Pass
        {
            Cull Off
            ZTest Always
        }
    }
}
