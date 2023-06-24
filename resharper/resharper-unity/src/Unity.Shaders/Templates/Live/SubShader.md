---
guid: F6E40E79-3DE2-4F89-AFE6-755F050F81D2
type: Live
reformat: True
shortenReferences: True
categories: unity
scopes: InUnityCSharpProject;MustBeInShaderLabBlock(blockKeyword=Shader)
---

# vfshader

ShaderLab Vertex/Fragment Shader

```
SubShader
{
    Tags { "RenderType"="Opaque" }    

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
}
```