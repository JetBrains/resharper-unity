---
guid: 93B0021B-1022-4261-BDC6-4E63701BAC91
type: Live
reformat: True
shortenReferences: True
categories: unity
scopes: InShaderLabBlock(blockKeyword=Shader)
---

# surf

ShaderLab Surface Shader

```
SubShader
{
    Tags { "RenderType" = "Opaque" }

    CGPROGRAM
    #pragma surface surf Lambert
    struct Input
    {
        float4 color : COLOR;
    };

    void surf(Input IN, inout SurfaceOutput o)
    {
        o.Albedo = 1;
    }
    ENDCG
}
```