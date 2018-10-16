---
guid: 7b10542b-0a61-4bd8-ba91-e5bad4d39f5b
image: UnityShaderLab
type: File
reformat: True
shortenReferences: True
categories: unity
customProperties: Extension=shader, FileName=NewImageEffectShader, ValidateFileName=True
scopes: InUnityCSharpAssetsFolder
parameterOrder: (NAME)
NAME-expression: getAlphaNumericFileNameWithoutExtension()
---

# Image Effect Shader

```
Shader "Hidden/$NAME$"
{
	$END$Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			sampler2D _MainTex;

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv);
				// just invert the colors
				col = 1 - col;
				return col;
			}
			ENDCG
		}
	}
}
```
