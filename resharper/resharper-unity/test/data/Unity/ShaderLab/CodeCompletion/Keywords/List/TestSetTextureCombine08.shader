Shader "MyShader" {
    SubShader {
        Pass
        {
            SetTexture [_ABC]
            {
                Combine Previous Lerp (One - Constant) {caret}
            }
        }
    }
}