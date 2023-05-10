Shader "MyShader" {
    SubShader {
        Pass
        {
            SetTexture [_ABC]
            {
                {caret}
            }
        }
    }
}