Shader "Unlit/NewUnlitShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    
    Category
    {
        Tags { ABC=BCD }
        Cull Off
        ZWrite Off
        Conservative Off
        ZTest Always
        ZClip False
        Offset 12, 12
        ColorMask 12
        Blend One Zero
        BlendOp Darken
        AlphaToMask Off
        Stencil 
        {
            Comp Always
            Fail Invert
            Pass Invert
            Ref 12
            ZFail Keep
        }
        Name "ABC"
        LOD 100
        Color (1, 2, 3)
        Lighting Off
        SeparateSpecular Off
        ColorMaterial Emission
        Material 
        {
            Ambient (0, 0.5, 0.3)
            Diffuse (0.1, 0.2, 0.3) 
        }
        AlphaTest Always 12
        Fog 
        {
            Color (0, 0.5, 0.3)
            Density 12
            Mode Global
            Range 12, 12
        }
        
        BindChannels 
        {
            Bind "Test", ABC
        }
        
        SubShader 
        {
            Cull Off
            ZWrite Off
        }
        
        Fallback "ABC"
        Dependency "ABC"="BCD"
        CustomEditor "MyCustomEditor"
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }
        LOD 100

        Pass 
        {
            Name "ABC"
            
            SetTexture [_MainTex] 
            {
                ConstantColor (0.1, 0.2, 0.3)
                Combine Constant Dot3 Previous Alpha
                Matrix [_MainTex]
            }
        }
        
        GrabPass 
        {
            Name "My Pass"
            Tags { ABC=BCD }
        }
        
        UsePass "ABC"
    }
    
    Fallback "ABC"
    Dependency "ABC"="BCD"
    CustomEditor "MyCustomEditor"
}
