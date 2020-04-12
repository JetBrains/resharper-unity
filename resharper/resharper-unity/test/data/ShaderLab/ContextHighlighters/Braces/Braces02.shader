// ${MatchingBracePosition:BOTH_SIDES}
Shader "Foo" {caret:LBraceOuter}{
  Properties {{caret:LBraceInner}
    _Color("Color", Color) = (1,1,1,1)
    _MainText("Albedo", 2D) = "white" {}
  }

  CGINCLUDE
  ENDCG

  SubShader {
    Tags { "Queue" = "Transparent" }
    LOD 300

    Pass {
      Name "Thing"
      Tags { "LightMode" = "ForwardBase" }

      Blend [_SrcBlend] [_DstBlend]
      ZWrite [_ZWrite]

      CGPROGRAM
#pragma target 3.0

      ENDCG
    {caret:RBraceInner}}
  }{caret:RBraceOuter}
}
