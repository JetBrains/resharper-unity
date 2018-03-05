Shader "Foo"
{ 
	Properties
	{
		_C{caret:Color}olor ("Color", Color) = (1,1,1,1)
		_Src{caret:SrcBlend}Blend ("SrcBlend", Int) = 5.0
		_DstBlend ("Ds{caret:Nothing}tBlend", Int) = 10.0
		_ZWrite ("ZWrite", I{caret:StillNothing}nt) = 1.0
		_ZTest ("ZTest", Int) = 4.0
		_Cull ("Cull", Int) = 0.0
		_Z{caret:ZBias}Bias ("ZBias", Float) = 0.0
	}

	SubShader
	{
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
		Pass
		{
			Blend [_SrcBlend] [_DstBlend]
			ZWrite [_ZWrite]
			ZTest [_ZTest]
			Cull [_Cull]
			Offset [_ZBias], [_ZBias]

			CGPROGRAM
			ENDCG  
		}  
	}
}
