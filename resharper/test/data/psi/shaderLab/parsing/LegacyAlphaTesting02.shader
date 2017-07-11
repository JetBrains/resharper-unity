{caret}Shader "Cutoff Alpha" {
    Properties {
        _MainTex ("Base (RGB) Transparency (A)", 2D) = "" {}
        _Cutoff ("Alpha cutoff", Range (0,1)) = 0.5
    }
    SubShader {
        Pass {
            // Use the Cutoff parameter defined above to determine
            // what to render.
            AlphaTest Greater [_Cutoff]
            Material {
                Diffuse (1,1,1,1)
                Ambient (1,1,1,1)
            }
            Lighting On
            SetTexture [_MainTex] { combine texture * primary }
        }
    }
}
