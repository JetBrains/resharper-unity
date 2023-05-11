Shader "MyShader" {
    SubShader {
        Pass
        {
            SetTexture [_ABC]
            {
                Matrix [unity_LightmapMatrix]
                {caret}
            }
        }
    }
}