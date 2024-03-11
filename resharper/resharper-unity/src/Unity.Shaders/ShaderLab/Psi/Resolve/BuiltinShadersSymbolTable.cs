#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Xml;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.DeclaredElements;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Dependencies;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.DataStructures;
using JetBrains.Util.DataStructures.Collections;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Resolve;

public class BuiltinShadersSymbolTable(IPsiServices psiServices) : SymbolTableBase
{
    // List of all known built-in shaders for 2023.2.12f1
    // You can use tools/BuiltinShadersExtractor for updating the list    
    // ReSharper disable StringLiteralTypo
    private static readonly IReadOnlyList<string> ourShaderNames = ImmutableArray.Create(
        "Autodesk Interactive",
        "Editor/Bumped Specular",
        "Editor/Diffuse",
        "Editor/Transparent/Cutout/Diffuse",
        "Editor/Transparent/Diffuse",
        "FX/Flare",
        "GUI/Text Shader",
        "Hidden/2D Handles Dotted Lines",
        "Hidden/2D Handles Lines",
        "Hidden/AlphaBasedSelection",
        "Hidden/AlphaBasedSelectionNoZWrite",
        "Hidden/AnimationWindowControlPoint",
        "Hidden/AnimationWindowCurve",
        "Hidden/BlitCopy",
        "Hidden/BlitCopyDepth",
        "Hidden/BlitCopyHDRTonemap",
        "Hidden/BlitCopyHDRTonemappedToHDRTonemap",
        "Hidden/BlitCopyHDRTonemappedToSDR",
        "Hidden/BlitCopyWithDepth",
        "Hidden/BlitSceneViewCapture",
        "Hidden/BlitToDepth",
        "Hidden/BlitToDepth_MSAA",
        "Hidden/BoneHandles",
        "Hidden/Compositing",
        "Hidden/ConvertTexture",
        "Hidden/CubeBlend",
        "Hidden/CubeBlur",
        "Hidden/CubeBlurOdd",
        "Hidden/CubeCopy",
        "Hidden/Editor Gizmo",
        "Hidden/Editor Gizmo",
        "Hidden/Editor Gizmo Color Occlusion",
        "Hidden/Editor Gizmo Icon Picking",
        "Hidden/Editor Gizmo Lit",
        "Hidden/Editor Gizmo Text",
        "Hidden/Editor Gizmo Textured",
        "Hidden/FrameDebuggerRenderTargetDisplay",
        "Hidden/GIDebug/ShowLightMask",
        "Hidden/GIDebug/TextureUV",
        "Hidden/GIDebug/UV1sAsPositions",
        "Hidden/GIDebug/VertexColors",
        "Hidden/GraphView/AAEdge",
        "Hidden/GUITextureBlit2Linear",
        "Hidden/GUITextureBlit2SRGB",
        "Hidden/GUITextureBlitSceneGUI",
        "Hidden/Handles Circular Arc",
        "Hidden/Handles Dotted Lines",
        "Hidden/Handles Icon",
        "Hidden/Handles Lines",
        "Hidden/Handles Shaded",
        "Hidden/Highlight Backfaces",
        "Hidden/Internal-Colored",
        "Hidden/Internal-CombineDepthNormals",
        "Hidden/Internal-CubemapToEquirect",
        "Hidden/Internal-DebugPattern",
        "Hidden/Internal-DeferredReflections",
        "Hidden/Internal-DeferredShading",
        "Hidden/Internal-DepthNormalsTexture",
        "Hidden/Internal-Flare",
        "Hidden/Internal-GUIRoundedRect",
        "Hidden/Internal-GUIRoundedRectWithColorPerBorder",
        "Hidden/Internal-GUITexture",
        "Hidden/Internal-GUITextureBlit",
        "Hidden/Internal-GUITextureClip",
        "Hidden/Internal-GUITextureClipInactive",
        "Hidden/Internal-GUITextureClipText",
        "Hidden/Internal-GUITextureClipVertically",
        "Hidden/Internal-Halo",
        "Hidden/Internal-Loading",
        "Hidden/Internal-MotionVectors",
        "Hidden/Internal-ODSWorldTexture",
        "Hidden/Internal-ScreenSpaceShadows",
        "Hidden/Internal-StencilWrite",
        "Hidden/InternalClear",
        "Hidden/InternalErrorShader",
        "Hidden/Light Probe Group Tetrahedra",
        "Hidden/Light Probe Handles Shaded",
        "Hidden/Light Probe Wire",
        "Hidden/Mesh-MultiPreview",
        "Hidden/Nature/Terrain/Utilities",
        "Hidden/Nature/Tree Creator Albedo Rendertex",
        "Hidden/Nature/Tree Creator Bark Optimized",
        "Hidden/Nature/Tree Creator Bark Rendertex",
        "Hidden/Nature/Tree Creator Leaves Fast Optimized",
        "Hidden/Nature/Tree Creator Leaves Optimized",
        "Hidden/Nature/Tree Creator Leaves Rendertex",
        "Hidden/Nature/Tree Creator Normal Rendertex",
        "Hidden/Nature/Tree Soft Occlusion Bark Rendertex",
        "Hidden/Nature/Tree Soft Occlusion Leaves Rendertex",
        "Hidden/OpaqueSelection",
        "Hidden/ParticleShapeGizmo",
        "Hidden/ParticleShapeGizmoSphere",
        "Hidden/Preview 2D Texture Array",
        "Hidden/Preview Alpha",
        "Hidden/Preview Alpha VT",
        "Hidden/Preview AudioClip Waveform",
        "Hidden/Preview Color2D",
        "Hidden/Preview Color2D VT",
        "Hidden/Preview Cubemap",
        "Hidden/Preview Encoded Lightmap doubleLDR",
        "Hidden/Preview Encoded Lightmap HDR",
        "Hidden/Preview Encoded Lightmap RGBM",
        "Hidden/Preview Encoded Normals",
        "Hidden/Preview Encoded Normals VT",
        "Hidden/Preview Plane With Shadow",
        "Hidden/Preview Shadow Mask",
        "Hidden/Preview Shadow Plane Clip",
        "Hidden/Preview Transparent",
        "Hidden/Preview Transparent VT",
        "Hidden/Scene View Show Mips",
        "Hidden/Scene View Show Overdraw",
        "Hidden/Scene View Show Texture Streaming",
        "Hidden/SceneColoredTexture",
        "Hidden/Sceneview Alpha Shader",
        "Hidden/SceneView grid",
        "Hidden/SceneView grid ortho",
        "Hidden/SceneView/GridGap",
        "Hidden/SceneViewApplyFilter",
        "Hidden/SceneViewAura",
        "Hidden/SceneViewBuildFilter",
        "Hidden/SceneViewDeferredBuffers",
        "Hidden/SceneViewGrayscaleEffectFade",
        "Hidden/SceneViewSelected",
        "Hidden/SceneViewWireframe",
        "Hidden/SeparableBlur",
        "Hidden/SH",
        "Hidden/Show Lightmap Resolution",
        "Hidden/ShowOverlap",
        "Hidden/ShowShadowCascadeSplits",
        "Hidden/TerrainEngine/BillboardTree",
        "Hidden/TerrainEngine/BrushPreview",
        "Hidden/TerrainEngine/CameraFacingBillboardTree",
        "Hidden/TerrainEngine/CrossBlendNeighbors",
        "Hidden/TerrainEngine/Details/BillboardWavingDoublePass",
        "Hidden/TerrainEngine/Details/Vertexlit",
        "Hidden/TerrainEngine/Details/WavingDoublePass",
        "Hidden/TerrainEngine/GenerateNormalmap",
        "Hidden/TerrainEngine/HeightBlitCopy",
        "Hidden/TerrainEngine/PaintHeight",
        "Hidden/TerrainEngine/Splatmap/Diffuse-AddPass",
        "Hidden/TerrainEngine/Splatmap/Diffuse-Base",
        "Hidden/TerrainEngine/Splatmap/Diffuse-BaseGen",
        "Hidden/TerrainEngine/Splatmap/Specular-AddPass",
        "Hidden/TerrainEngine/Splatmap/Specular-Base",
        "Hidden/TerrainEngine/Splatmap/Standard-AddPass",
        "Hidden/TerrainEngine/Splatmap/Standard-Base",
        "Hidden/TerrainEngine/Splatmap/Standard-BaseGen",
        "Hidden/TerrainEngine/TerrainBlitCopyZWrite",
        "Hidden/TerrainEngine/TerrainLayerUtils",
        "Hidden/TextCore/Distance Field",
        "Hidden/TextCore/Distance Field SSD",
        "Hidden/TextCore/Editor/Distance Field SSD",
        "Hidden/TextCore/Editor/Sprite",
        "Hidden/TextCore/Sprite",
        "Hidden/TreeTextureCombiner Shader",
        "Hidden/UI/CompositeOverdraw",
        "Hidden/UI/Overdraw",
        "Hidden/UIElements/AACurveField",
        "hidden/Unlit/Avatar",
        "hidden/Unlit/Avatar-Transparent",
        "Hidden/VertexSelected",
        "Hidden/VertexSelection",
        "Hidden/VertexSelectionBackfaces",
        "Hidden/VideoComposite",
        "Hidden/VideoDecode",
        "Hidden/VideoDecodeAndroid",
        "Hidden/VideoDecodeOSX",
        "Hidden/VR/BlitCopyHDRTonemappedToHDRTonemapTexArraySlice",
        "Hidden/VR/BlitCopyHDRTonemappedToSDRTexArraySlice",
        "Hidden/VR/BlitCopyHDRTonemapTexArraySlice",
        "Hidden/VR/BlitFromTex2DToTexArraySlice",
        "Hidden/VR/BlitTexArraySlice",
        "Hidden/VR/BlitTexArraySliceToDepth",
        "Hidden/VR/BlitTexArraySliceToDepth_MSAA",
        "Hidden/VR/Internal-VRDistortion",
        "Legacy Shaders/Bumped Diffuse",
        "Legacy Shaders/Bumped Specular",
        "Legacy Shaders/Decal",
        "Legacy Shaders/Diffuse",
        "Legacy Shaders/Diffuse Detail",
        "Legacy Shaders/Diffuse Fast",
        "Legacy Shaders/Lightmapped/Bumped Diffuse",
        "Legacy Shaders/Lightmapped/Bumped Specular",
        "Legacy Shaders/Lightmapped/Diffuse",
        "Legacy Shaders/Lightmapped/Specular",
        "Legacy Shaders/Lightmapped/VertexLit",
        "Legacy Shaders/Parallax Diffuse",
        "Legacy Shaders/Parallax Specular",
        "Legacy Shaders/Particles/~Additive-Multiply",
        "Legacy Shaders/Particles/Additive",
        "Legacy Shaders/Particles/Additive (Soft)",
        "Legacy Shaders/Particles/Alpha Blended",
        "Legacy Shaders/Particles/Alpha Blended Premultiply",
        "Legacy Shaders/Particles/Anim Alpha Blended",
        "Legacy Shaders/Particles/Blend",
        "Legacy Shaders/Particles/Multiply",
        "Legacy Shaders/Particles/Multiply (Double)",
        "Legacy Shaders/Particles/VertexLit Blended",
        "Legacy Shaders/Reflective/Bumped Diffuse",
        "Legacy Shaders/Reflective/Bumped Specular",
        "Legacy Shaders/Reflective/Bumped Unlit",
        "Legacy Shaders/Reflective/Bumped VertexLit",
        "Legacy Shaders/Reflective/Diffuse",
        "Legacy Shaders/Reflective/Parallax Diffuse",
        "Legacy Shaders/Reflective/Parallax Specular",
        "Legacy Shaders/Reflective/Specular",
        "Legacy Shaders/Reflective/VertexLit",
        "Legacy Shaders/Self-Illumin/Bumped Diffuse",
        "Legacy Shaders/Self-Illumin/Bumped Specular",
        "Legacy Shaders/Self-Illumin/Diffuse",
        "Legacy Shaders/Self-Illumin/Parallax Diffuse",
        "Legacy Shaders/Self-Illumin/Parallax Specular",
        "Legacy Shaders/Self-Illumin/Specular",
        "Legacy Shaders/Self-Illumin/VertexLit",
        "Legacy Shaders/Specular",
        "Legacy Shaders/Transparent/Bumped Diffuse",
        "Legacy Shaders/Transparent/Bumped Specular",
        "Legacy Shaders/Transparent/Cutout/Bumped Diffuse",
        "Legacy Shaders/Transparent/Cutout/Bumped Specular",
        "Legacy Shaders/Transparent/Cutout/Diffuse",
        "Legacy Shaders/Transparent/Cutout/Soft Edge Unlit",
        "Legacy Shaders/Transparent/Cutout/Specular",
        "Legacy Shaders/Transparent/Cutout/VertexLit",
        "Legacy Shaders/Transparent/Diffuse",
        "Legacy Shaders/Transparent/Parallax Diffuse",
        "Legacy Shaders/Transparent/Parallax Specular",
        "Legacy Shaders/Transparent/Specular",
        "Legacy Shaders/Transparent/VertexLit",
        "Legacy Shaders/VertexLit",
        "Mobile/Bumped Diffuse",
        "Mobile/Bumped Specular",
        "Mobile/Bumped Specular (1 Directional Realtime Light)",
        "Mobile/Diffuse",
        "Mobile/Particles/Additive",
        "Mobile/Particles/Alpha Blended",
        "Mobile/Particles/Multiply",
        "Mobile/Particles/VertexLit Blended",
        "Mobile/Skybox",
        "Mobile/Unlit (Supports Lightmap)",
        "Mobile/VertexLit",
        "Mobile/VertexLit (Only Directional Lights)",
        "Nature/SpeedTree",
        "Nature/SpeedTree Billboard",
        "Nature/SpeedTree8",
        "Nature/Terrain/Diffuse",
        "Nature/Terrain/Specular",
        "Nature/Terrain/Standard",
        "Nature/Tree Creator Bark",
        "Nature/Tree Creator Leaves",
        "Nature/Tree Creator Leaves Fast",
        "Nature/Tree Soft Occlusion Bark",
        "Nature/Tree Soft Occlusion Leaves",
        "Particles/Standard Surface",
        "Particles/Standard Unlit",
        "Preview/CubemapArray",
        "Skybox/6 Sided",
        "Skybox/Cubemap",
        "Skybox/Panoramic",
        "Skybox/Procedural",
        "Sprites/Default",
        "Sprites/Diffuse",
        "Sprites/Mask",
        "Standard",
        "Standard (Specular setup)",
        "UI/Default",
        "UI/Default Font",
        "UI/DefaultETC1",
        "UI/Lit/Bumped",
        "UI/Lit/Detail",
        "UI/Lit/Refraction",
        "UI/Lit/Refraction Detail",
        "UI/Lit/Transparent",
        "UI/Unlit/Detail",
        "UI/Unlit/Text",
        "UI/Unlit/Text Detail",
        "UI/Unlit/Transparent",
        "Unlit/Color",
        "Unlit/Preview3DSDF",
        "Unlit/Preview3DSliced",
        "Unlit/Preview3DVolume",
        "Unlit/Texture",
        "Unlit/Transparent",
        "Unlit/Transparent Cutout",
        "VR/SpatialMapping/Occlusion",
        "VR/SpatialMapping/Wireframe"
        // ReSharper restore StringLiteralTypo
    );
    
    private readonly Dictionary<string, ISymbolInfo> mySymbolInfos = BuildSymbolInfos(psiServices);

    private static Dictionary<string, ISymbolInfo> BuildSymbolInfos(IPsiServices psiServices)
    {
        var result = new Dictionary<string, ISymbolInfo>(ourShaderNames.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var shaderName in ourShaderNames) 
            result[shaderName] = new SymbolInfo(shaderName, new BuiltinShaderDeclaredElement(shaderName, psiServices), EmptySubstitution.INSTANCE, -1, 0);
        return result;
    }

    public override IEnumerable<string> Names() => ourShaderNames;

    public override IList<ISymbolInfo> GetSymbolInfos(string name) => (IList<ISymbolInfo>)(mySymbolInfos.TryGetValue(name, out var value) ? FixedList.Of(value) : EmptyList<ISymbolInfo>.Instance);

    public override void ForAllSymbolInfos(Action<ISymbolInfo> processor)
    {
        foreach (var symbol in mySymbolInfos.Values)
            processor(symbol);
    }

    public override ISymbolTableDependencySet? GetDependencySet() => null;

    private class BuiltinShaderDeclaredElement(string name, IPsiServices psiServices) : IShaderLabDeclaredElement
    {
        public string ShortName => name;
        public bool CaseSensitiveName => false;
        public PsiLanguageType PresentationLanguage => ShaderLabLanguage.Instance!;
        public DeclaredElementType GetElementType() => ShaderLabDeclaredElementType.Shader;

        public bool IsValid() => true;

        public bool IsSynthetic() => false;

        public IList<IDeclaration> GetDeclarations() => EmptyList<IDeclaration>.Instance;

        public IList<IDeclaration> GetDeclarationsIn(IPsiSourceFile sourceFile) => EmptyList<IDeclaration>.Instance;

        public HybridCollection<IPsiSourceFile> GetSourceFiles() => HybridCollection<IPsiSourceFile>.Empty;

        public bool HasDeclarationsIn(IPsiSourceFile sourceFile) => false;

        public IPsiServices GetPsiServices() => psiServices;

        public XmlNode? GetXMLDoc(bool inherit) => null;

        public XmlNode? GetXMLDescriptionSummary(bool inherit) => null;
    }
}