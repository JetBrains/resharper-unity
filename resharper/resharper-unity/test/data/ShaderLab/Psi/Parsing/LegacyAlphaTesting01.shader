{caret}Shader "Simple Alpha Test" {
    Properties {
        _MainTex ("Base (RGB) Transparency (A)", 2D) = "" {}
    }
    SubShader {
        Pass {
            // Only render pixels with an alpha larger than 50%
            AlphaTest Greater 0.5
            SetTexture [_MainTex] { combine texture }
        }
        Pass {
            AlphaTest Off
            SetTexture [_MainTex] { combine texture }
        }
        Pass {
            // Treated as LEqual
            AlphaTest True 0
            SetTexture [_MainTex] { combine texture }
        }
    }
}
