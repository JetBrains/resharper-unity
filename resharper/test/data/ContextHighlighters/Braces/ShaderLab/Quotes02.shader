// ${MatchingBracePosition:BOTH_SIDES}
Shader {caret:LQuoteOuter}"Foo" {
  Properties {
    _Color("{caret:LQuoteInner}Color", Color) = (1,1,1,1)
    _MainText("Albedo{caret:RQuoteInner}", 2D) = "white" {}
    _MainText("Albedo2"{caret:RQuoteOuter}, 2D) = "white" {}
  }
}
