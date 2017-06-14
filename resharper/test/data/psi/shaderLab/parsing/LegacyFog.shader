{caret}Shader "Solid Red"
{
    Properties {
        _Color ("Main Color", COLOR) = (1,1,1,1)
        _Density ("Density", Range (0.01, 1)) = 0.7
        _Min ("Min", Range (0.01, 1)) = 0.7
        _Max ("Max", Range (0.01, 1)) = 0.7
    }
    SubShader
    {
        Pass
        {
            Fog { Mode Off }
        }
        Pass
        {
            Fog
            {
                Mode Global
                Color (1, 1, 1, 1)
                Density 134
                Range 120, 140
            }
        }
        Pass
        {
            Fog
            {
                Mode Exp2
                Color [_Color]
                Density [_Density]
                Range [_Min], [_Max]
            }
        }
    }
}
