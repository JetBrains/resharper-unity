Shader "MyShader"
{
    Properties
    {
        [HideInInspector] _Color ("Main Color", Color) = (1,1,1,1)
        _SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
        [HideInInspector] _GlossMap ("Gloss Map", 3D) = "gloss" {}
        [Enum(UV0,0,UV1,1)] _UVSec ("UV Set for secondary textures", Float) = 0
    }
}
