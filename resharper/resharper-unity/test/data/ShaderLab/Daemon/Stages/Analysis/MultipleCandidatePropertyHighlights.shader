Shader "Foo"
{ 
	Properties
	{
		_Color ("Color", Color) = (1,1,1,1)
		_SrcBlend ("SrcBlend", Int) = 5.0
		_DstBlend ("DstBlend", Int) = 10.0
		_ZWrite ("ZWrite", Int) = 1.0
		_ZTest ("ZTest", Int) = 4.0
		_Cull ("Cull", Int) = 0.0
		_ZBias ("ZBias", Float) = 0.0
		_ZBias ("Duplicate ZBias property", Float) = 0.0
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
