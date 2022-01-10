// ${MatchingBracePosition:BOTH_SIDES}
Shader "Foo" {
  Properties {
    _Color{caret:LParenOuter}("Color", Color) = (1,1,1,1{caret:RParenInner})
    _Color2("Color2", Color) = (1,1,1,1){caret:RParenOuter}
    _MainText({caret:LParenInner} "Albedo", 2D) = "white" {}
  }
}
