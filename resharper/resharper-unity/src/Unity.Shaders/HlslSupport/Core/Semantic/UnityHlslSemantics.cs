#nullable enable
using System.Collections.Immutable;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Core.Types;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Core.Semantic
{
    public static class UnityHlslSemantics
    {
        public static readonly ImmutableArray<HlslSemantic> All = ImmutableArray.Create(
            // Vertex inputs semantic 
            new HlslSemantic("POSITION", HlslSemanticScope.VertexInput,  HlslWellKnownTypes.Half3, HlslWellKnownTypes.Half4, HlslWellKnownTypes.Float3, HlslWellKnownTypes.Float4), // POSITION is the vertex position, typically a float3 or float4.
            new HlslSemantic("NORMAL", HlslSemanticScope.VertexInput, HlslWellKnownTypes.Half3, HlslWellKnownTypes.Float3), // NORMAL is the vertex normal, typically a float3.
            new HlslSemantic("TEXCOORD0", HlslSemanticScope.VertexInput | HlslSemanticScope.VertexOutput, HlslWellKnownTypes.Half2, HlslWellKnownTypes.Half3, HlslWellKnownTypes.Half4, HlslWellKnownTypes.Float2, HlslWellKnownTypes.Float3, HlslWellKnownTypes.Float4), // TEXCOORD0 is the first UV coordinate, typically float2, float3 or float4.
            new HlslSemantic("TEXCOORD1", HlslSemanticScope.VertexInput | HlslSemanticScope.VertexOutput, HlslWellKnownTypes.Half2, HlslWellKnownTypes.Half3, HlslWellKnownTypes.Half4, HlslWellKnownTypes.Float2, HlslWellKnownTypes.Float3, HlslWellKnownTypes.Float4), // TEXCOORD1, TEXCOORD2 and TEXCOORD3 are the 2nd, 3rd and 4th UV coordinates, respectively.
            new HlslSemantic("TEXCOORD2", HlslSemanticScope.VertexInput | HlslSemanticScope.VertexOutput, HlslWellKnownTypes.Half2, HlslWellKnownTypes.Half3, HlslWellKnownTypes.Half4, HlslWellKnownTypes.Float2, HlslWellKnownTypes.Float3, HlslWellKnownTypes.Float4), //
            new HlslSemantic("TEXCOORD3", HlslSemanticScope.VertexInput | HlslSemanticScope.VertexOutput, HlslWellKnownTypes.Half2, HlslWellKnownTypes.Half3, HlslWellKnownTypes.Half4, HlslWellKnownTypes.Float2, HlslWellKnownTypes.Float3, HlslWellKnownTypes.Float4), //
            new HlslSemantic("TANGENT", HlslSemanticScope.VertexInput, HlslWellKnownTypes.Half4, HlslWellKnownTypes.Float4), // TANGENT is the tangent vector (used for normal mapping), typically a float4.
            new HlslSemantic("COLOR", HlslSemanticScope.VertexInput, HlslWellKnownTypes.Half4, HlslWellKnownTypes.Float4), // COLOR is the per-vertex color, typically a float4.
            // Vertex outputs/Fragment inputs semantic
            new HlslSemantic("SV_POSITION", HlslSemanticScope.VertexOutput, HlslWellKnownTypes.Half4, HlslWellKnownTypes.Float4), // A vertex shader needs to output the final clip space position of a vertex, so that the GPU knows where on the screen to rasterize it, and at what depth. This output needs to have the SV_POSITION semantic, and be of a float4 type.
            new HlslSemantic("TEXCOORD4", HlslSemanticScope.VertexOutput, HlslWellKnownTypes.Half2, HlslWellKnownTypes.Half3, HlslWellKnownTypes.Half4, HlslWellKnownTypes.Float2, HlslWellKnownTypes.Float3, HlslWellKnownTypes.Float4), // TEXCOORD0, TEXCOORD1 etc are used to indicate arbitrary high precision data such as texture coordinates and positions.
            new HlslSemantic("TEXCOORD5", HlslSemanticScope.VertexOutput, HlslWellKnownTypes.Half2, HlslWellKnownTypes.Half3, HlslWellKnownTypes.Half4, HlslWellKnownTypes.Float2, HlslWellKnownTypes.Float3, HlslWellKnownTypes.Float4), // 
            new HlslSemantic("TEXCOORD6", HlslSemanticScope.VertexOutput, HlslWellKnownTypes.Half2, HlslWellKnownTypes.Half3, HlslWellKnownTypes.Half4, HlslWellKnownTypes.Float2, HlslWellKnownTypes.Float3, HlslWellKnownTypes.Float4), //
            new HlslSemantic("TEXCOORD7", HlslSemanticScope.VertexOutput, HlslWellKnownTypes.Half2, HlslWellKnownTypes.Half3, HlslWellKnownTypes.Half4, HlslWellKnownTypes.Float2, HlslWellKnownTypes.Float3, HlslWellKnownTypes.Float4), //
            new HlslSemantic("COLOR0", HlslSemanticScope.VertexOutput, HlslWellKnownTypes.Half4, HlslWellKnownTypes.Float4), // COLOR0 and COLOR1 semantics on vertex outputs and fragment inputs are for low-precision, 0–1 range data (like simple color values).
            new HlslSemantic("COLOR1", HlslSemanticScope.VertexOutput, HlslWellKnownTypes.Half4, HlslWellKnownTypes.Float4),
            new HlslSemantic("COLOR2", HlslSemanticScope.VertexOutput, HlslWellKnownTypes.Half4, HlslWellKnownTypes.Float4),
            new HlslSemantic("COLOR3", HlslSemanticScope.VertexOutput, HlslWellKnownTypes.Half4, HlslWellKnownTypes.Float4),
            new HlslSemantic("COLOR4", HlslSemanticScope.VertexOutput, HlslWellKnownTypes.Half4, HlslWellKnownTypes.Float4),
            new HlslSemantic("COLOR5", HlslSemanticScope.VertexOutput, HlslWellKnownTypes.Half4, HlslWellKnownTypes.Float4),
            new HlslSemantic("COLOR6", HlslSemanticScope.VertexOutput, HlslWellKnownTypes.Half4, HlslWellKnownTypes.Float4),
            new HlslSemantic("COLOR7", HlslSemanticScope.VertexOutput, HlslWellKnownTypes.Half4, HlslWellKnownTypes.Float4)
            // Fragment outputs are defined via macro in Unity, no need to add separate completion
        );
    }
}