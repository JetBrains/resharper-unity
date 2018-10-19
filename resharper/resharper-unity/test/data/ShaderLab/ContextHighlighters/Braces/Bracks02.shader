// ${MatchingBracePosition:BOTH_SIDES}
Shader "Foo" {
  Properties {
    {caret:LOuter}[Hidden] _Color("Color", Color) = (1,1,1,1)
    [{caret:LInner}Hidden] _Color2("Color2", Color) = (1,1,1,1)
    _MainText("Albedo", 2D) = "white" {}
  }

  SubShader {
    Tags { "Queue" = "Transparent" }
    LOD 300

    Pass {
      Name "Thing"
      Tags { "LightMode" = "ForwardBase" }

      Blend [_SrcBlend]{caret:ROuter} [_DstBlend]
      ZWrite [_ZWrite{caret:RInner}]

      CGPROGRAM
#pragma target 3.0

      ENDCG
    }
  }
}
