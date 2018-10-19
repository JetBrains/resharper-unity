{caret}Shader "VertexLit Simple" {
    Properties {
        _Color ("Main Color", COLOR) = (1,1,1,1)
	    _ColorComponent("Color Component", Float) = 0.65
    }
    SubShader {
        Pass {
            Material {
                Diffuse [_Color]
                Ambient (1, 1, 1, [_ColorComponent])
            }
            Lighting On
        }
    }
}
