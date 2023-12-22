Shader "Examples/2 Alpha Blended Textures" {
    Properties {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _BlendTex ("Alpha Blended (RGBA) ", 2D) = "white" {}
    }
    SubShader {
        Pass {
            // Test cases with one minus
            SetTexture [_MainText] {
                ConstantColor (0, 0.5, 0.75, 1)
                // lerp
                combine one-constant lerp(texture) previous
                combine constant lerp(texture) one-previous
                combine one-constant lerp(texture) one-previous
                
                // ops
                combine one-constant
                combine one-constant + one-previous
                combine one-constant - one-previous
                combine one-constant * one-previous
                combine one-constant * one-previous + one-primary
                combine one-constant * one-previous - one-primary
            }
            
            // Valid 3 arg ops
            SetTexture [_MainText] {
                ConstantColor (0, 0.5, 0.75, 1)
                
                combine constant * previous + primary
                
                // legacy
                combine constant * previous - primary
                combine constant +- primary
                combine constant * Previous +- primary
                combine constant dot3 Previous
                Combine Constant Dot3Rgba Primary Double
            }
            
            // Invalid ops
            SetTexture [_MainText] {
                ConstantColor (0, 0.5, 0.75, 1)
                
                combine constant lerp(texture) previous // one minus not allowed for lerp argument
                // only * followed by +/- allowed for 3 arg ops
                combine constant + previous - primary
                combine constant + previous * primary
                combine constant * previous * primary
            }
         }
    }
}
