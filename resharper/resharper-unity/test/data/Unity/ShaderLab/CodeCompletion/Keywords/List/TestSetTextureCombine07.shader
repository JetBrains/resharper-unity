Shader "MyShader" {
    SubShader {
        Pass
        {
            SetTexture [_ABC]
            {
                Combine Previous Lerp ({caret}
            }
        }
    }
}