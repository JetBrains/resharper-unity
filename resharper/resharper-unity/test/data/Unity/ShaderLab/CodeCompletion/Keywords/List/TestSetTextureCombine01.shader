Shader "MyShader" {
    SubShader {
        Pass
        {
            SetTexture [_ABC]
            {
                Combine {caret}
            }
        }
    }
}