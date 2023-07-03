---
guid: 32259FE9-7FC2-4B47-880B-C128F0DBF7F6
type: Live
reformat: True
shortenReferences: True
categories: unity
scopes: InShaderLabBlock(blockKeyword=SubShader)
---

# vfpass

ShaderLab Vertex/Fragment Shader Pass

```
Pass
{
    CGPROGRAM
    #pragma vertex vert
    #pragma fragment frag
    
    #include "UnityCG.cginc"

    struct appdata
    {
        float4 vertex : POSITION;
    };

    struct v2f
    {                        
        float4 vertex : SV_POSITION;
    };
   
    v2f vert (appdata v)
    {
        v2f o;
        o.vertex = UnityObjectToClipPos(v.vertex);            
        return o;
    }
    
    half4 frag (v2f i) : SV_Target
    {            
        return 1;
    }
    ENDCG
}
```