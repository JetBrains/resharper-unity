Shader "MyShader"
{
  Properties {
    // Make sure to include trailing period. This is a valid float, apparently
    _Color ("Main Color", Color) = (1, 1., .5, 1)
  }
}
