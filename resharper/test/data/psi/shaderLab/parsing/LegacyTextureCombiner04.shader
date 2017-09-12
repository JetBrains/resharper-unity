{caret}Shader "Examples/Self-Illumination 3" {
    Properties {
        _IlluminCol ("Self-Illumination color (RGB)", Color) = (1,1,1,1)
        _Color ("Main Color", Color) = (1,1,1,0)
        _SpecColor ("Spec Color", Color) = (1,1,1,1)
        _Emission ("Emmisive Color", Color) = (0,0,0,0)
        _Shininess ("Shininess", Range (0.01, 1)) = 0.7
        _MainTex ("Base (RGB)", 2D) = "white" {}
    }

    SubShader {
        Pass {
            // Set up basic vertex lighting
            Material {
                Diffuse [_Color]
                Ambient [_Color]
                Shininess [_Shininess]
                Specular [_SpecColor]
                Emission [_Emission]
            }
            Lighting On

            // Use texture alpha to blend up to white (= full illumination)
            SetTexture [_MainTex] {
                constantColor [_IlluminCol]
                combine constant lerp(texture) previous
            }

            // postmul
            SetTexture [_MainTex] {
                combine previous quad
                combine previous double
                combine previous * texture quad
                combine previous * texture alpha quad
            }

            // Operators
            SetTexture [_MainTex] {
                combine previous * texture + primary alpha
                combine previous alpha * texture alpha + primary alpha
                combine previous - texture
            }

            // Legacy operators
            SetTexture [_MainTex] {
                combine previous +- texture
                combine previous dot3 texture
                combine previous dot3rgba texture
                combine previous * texture +- primary alpha
            }
        }
    }
}
