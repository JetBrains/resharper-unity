Shader "MyShader" {
    Properties
    {
        _MyTexture("MyTexture", 2D) = "White"
    }
    
    SubShader {
        Pass
        {
            SetTexture [_MyTexture]
            {caret}
        }
    }
}