// Colored vertex lighting
Shader "MyShader"
{
  // a single color property
  Properties {
    _Color ("Main Color", Color) = (1, .5,.5,1)
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
      }
      Lighting On
    }
  }
}
