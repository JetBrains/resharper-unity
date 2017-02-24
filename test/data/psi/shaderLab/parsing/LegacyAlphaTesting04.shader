{caret}Shader "Simple Alpha Test" {
    Properties {
        _MainTex ("Base (RGB) Transparency (A)", 2D) = "" {}
    }
    SubShader {
        Pass {
            AlphaTest Off
            SetTexture [_MainTex] { combine texture }
        }
    }
}
