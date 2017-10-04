Shader "MyShader"
{
    Properties
    {
        _Color ("Main Color", Color) = (1,1,1,1)
        _SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
        _MyVector ("My Vector", Vector) = (0.5, 0.5, 0.5, 1)
        _Shininess ("Shininess", Range (0.01, 1)) = 0.078125
        _Value ("Value", Int) = 2
        _Value2 ("Value2", Float) = 0.3
        _Gloss ("Gloss", Range (0.0, 2.0)) = 1
        _MainTex ("Base Texture", 2D) = "white" {}
        _MainTex2 ("Terrain Texture Array", 2DArray) = "white" {}
        _BumpMap ("Normal Map", Cube) = "bump" {}
        _GlossMap ("Gloss Map", 3D) = "gloss" {}
    }
}
