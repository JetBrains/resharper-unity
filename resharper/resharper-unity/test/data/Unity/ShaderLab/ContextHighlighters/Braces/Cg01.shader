// ${MatchingBracePosition:OUTER_SIDE}
Shader "Foo" {
  Properties {
    _Color("Color", Color) = (1,1,1,1)
    _MainText("Albedo", 2D) = "white" {}
  }

  CGINC{caret:LMiddleInc}LUDE
  ENDCG

  CGINCLUDE{caret:LInnerInc}
  ENDCG

  {caret:LOuterInc}CGINCLUDE
  ENDCG

  CGINCLUDE
  ENDCG{caret:ROuterInc}

  CGINCLUDE
  {caret:RInnerInc}ENDCG

  CGINCLUDE
  END{caret:RMiddleInc}CG

  SubShader {
    Pass {
      CG{caret:LMiddleProg}PROGRAM
      ENDCG
    }

    Pass {
      CGPROGRAM{caret:LInnerProg}
      ENDCG
    }

    Pass {
      {caret:LOuterProg}CGPROGRAM
      ENDCG
    }

    Pass {
      CGPROGRAM
      ENDCG{caret:ROuterProg}
    }

    Pass {
      CGPROGRAM
      {caret:RInnerProg}ENDCG
    }

    Pass {
      CGPROGRAM
      EN{caret:RMiddleProg}DCG
    }
  }
}
