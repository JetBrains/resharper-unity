Shader "MyShader" {
    SubShader {
        Pass
        {
            SetTexture [_ABC]
            {
                Combine One-Previous {caret}
            }
        }
    }
}