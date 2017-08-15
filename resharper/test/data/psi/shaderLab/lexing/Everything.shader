// Colored vertex lighting
Shader "MyShader"
{
  // a single color property
  Properties {
    _Color ("Main Color", Color) = (1, .5,.5,1)
    #Color2 ("Main Color 2", Color) = (1, .5,.5,1) // Technically allowed, but makes no sense in Cg/Hlsl
    C_o_l#or3 ("Main Color 3", Color) = (1, .5, .5, 1)
  }
  // define one subshader
  SubShader
  {
    // a single pass in our subshader
    Pass
    {
      Material
      {
        Diffuse [_Color]
        Diffuse [#Color2]
        Diffuse [C_o_l#or3]
      }
      Lighting On
    }
  }
}
