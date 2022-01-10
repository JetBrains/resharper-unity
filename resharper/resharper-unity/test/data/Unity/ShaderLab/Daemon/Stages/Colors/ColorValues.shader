Shader "Test" {
  SubShader {
    Color (0.3, 0.4, 0.2, 0.9)

    Material {
      Diffuse (0.3, 0.4, 0.3, 1)
      Ambient (0.9, 0.2, 0.1, 0.5)
      Specular (1, 0.5, 0.2, 1)
      Emission (0.4, 0.3, 0.2, 0.5)
    }

    Pass {
      SetTexture [_Tex] {
        ConstantColor (0.7, 0.8, 0.3, 1)
      }
    }
  }
}
