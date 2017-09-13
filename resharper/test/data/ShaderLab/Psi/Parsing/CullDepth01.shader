{caret}Shader "Blah"
{
    Properties { _CullValue ("Cull value", Float) = 0 }
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
        Pass
        {
            Cull False
            // ZTest bool value not documented
            ZTest Off
            // ZClip not documented
            ZClip On
        }
        Pass
        {
            Cull [_CullValue]
            // ZTest bool value not documented
            ZTest True
            // ZClip not documented
            ZClip False
        }
    }
}
