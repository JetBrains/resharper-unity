#nullable enable
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport;
using JetBrains.ReSharper.Psi.Cpp.Language;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Language
{
    public sealed class UnityHlslPragmas : CppPragmas
    {
        public static readonly ShaderLabPragmaCommand MultiCompile = new("multi_compile", PragmaCommandFlags.HasRequiredSpec, new ShaderLabPragmaInfo { ShaderFeatureType = ShaderFeatureType.KeywordList });
        public static readonly PragmaCommand MultiCompileLocal = MultiCompile.WithSuffix("_local");
        public static readonly ShaderLabPragmaCommand ShaderFeature = new("shader_feature", PragmaCommandFlags.HasRequiredSpec, new ShaderLabPragmaInfo { ShaderFeatureType = ShaderFeatureType.KeywordListWithDisabledVariantForSingleKeyword });
        public static readonly PragmaCommand ShaderFeatureLocal = ShaderFeature.WithSuffix("_local");
        public static readonly ShaderLabPragmaCommand DynamicBranch = new("dynamic_branch", PragmaCommandFlags.HasRequiredSpec, new ShaderLabPragmaInfo { ShaderFeatureType = ShaderFeatureType.KeywordList });
        public static readonly PragmaCommand DynamicBranchLocal = DynamicBranch.WithSuffix("_local");
        public static readonly PragmaCommand EnableD3D11DebugSymbols = new("enable_d3d11_debug_symbols", PragmaCommandFlags.None);
        public static readonly PragmaCommand OnlyRenderers = new("only_renderers", PragmaCommandFlags.HasRequiredSpec);
        public static readonly PragmaCommand ExcludeRenderers = new("exclude_renderers", PragmaCommandFlags.HasRequiredSpec);
        public static readonly PragmaCommand Require = new("require", PragmaCommandFlags.HasRequiredSpec);
        public static readonly PragmaCommand SkipOptimizations = new("skip_optimizations", PragmaCommandFlags.HasRequiredSpec);
        public static readonly PragmaCommand DisableFastmath = new("disable_fastmath", PragmaCommandFlags.None);
        public static readonly PragmaCommand HlslccBytecodeDisassembly = new("hlslcc_bytecode_disassembly", PragmaCommandFlags.None);

        private static readonly ImmutableArray<PragmaCommand> ourUnityCommands = ImmutableArray.CreateRange(
            PragmaCommandEx.CreateArrayWithFlags(PragmaCommandFlags.None, "editor_sync_compilation", "enable_cbuffer")
                .Append(new ShaderLabPragmaCommand("mutli_compile_fwdbase", PragmaCommandFlags.None,
                    ShaderLabPragmaInfo.ForImplicitKeywordSet("DIRECTIONAL", "LIGHTMAP_ON", "DIRLIGHTMAP_COMBINED", "DYNAMICLIGHTMAP_ON", "SHADOWS_SCREEN", "SHADOWS_SHADOWMASK", "LIGHTMAP_SHADOW_MIXING", "LIGHTPROBE_SH")))
                .Append(new ShaderLabPragmaCommand("multi_compile_fwdbasealpha", PragmaCommandFlags.None,
                    ShaderLabPragmaInfo.ForImplicitKeywordSet("DIRECTIONAL", "LIGHTMAP_ON", "DIRLIGHTMAP_COMBINED", "DYNAMICLIGHTMAP_ON", "LIGHTMAP_SHADOW_MIXING", "VERTEXLIGHT_ON", "LIGHTPROBE_SH")))
                .Append(new ShaderLabPragmaCommand("multi_compile_fwdadd", PragmaCommandFlags.None,
                    ShaderLabPragmaInfo.ForImplicitKeywordSet("POINT", "DIRECTIONAL", "SPOT", "POINT_COOKIE", "DIRECTIONAL_COOKIE")))
                .Append(new ShaderLabPragmaCommand("multi_compile_fwdadd_fullshadows", PragmaCommandFlags.None,
                    ShaderLabPragmaInfo.ForImplicitKeywordSet("POINT", "DIRECTIONAL", "SPOT", "POINT_COOKIE", "DIRECTIONAL_COOKIE", "SHADOWS_DEPTH", "SHADOWS_SCREEN", "SHADOWS_CUBE", "SHADOWS_SOFT", "SHADOWS_SHADOWMASK", "LIGHTMAP_SHADOW_MIXING")))
                .Append(new ShaderLabPragmaCommand("multi_compile_lightpass", PragmaCommandFlags.None, 
                    ShaderLabPragmaInfo.ForImplicitKeywordSet("POINT", "DIRECTIONAL", "SPOT", "POINT_COOKIE", "DIRECTIONAL_COOKIE", "SHADOWS_DEPTH", "SHADOWS_SCREEN", "SHADOWS_CUBE", "SHADOWS_SOFT", "SHADOWS_SHADOWMASK", "LIGHTMAP_SHADOW_MIXING")))
                .Append(new ShaderLabPragmaCommand("multi_compile_shadowcaster", PragmaCommandFlags.None,
                    ShaderLabPragmaInfo.ForImplicitKeywordSet("SHADOWS_DEPTH", "SHADOWS_CUBE")))
                .Append(new ShaderLabPragmaCommand("multi_compile_shadowcollector", PragmaCommandFlags.None,
                    ShaderLabPragmaInfo.ForImplicitKeywordSetWithDisabledVariant("SHADOWS_SPLIT_SPHERES", "SHADOWS_SINGLE_CASCADE")))
                .Append(new ShaderLabPragmaCommand("multi_compile_prepassfinal", PragmaCommandFlags.None,
                    ShaderLabPragmaInfo.ForImplicitKeywordSetWithDisabledVariant("LIGHTMAP_ON", "DIRLIGHTMAP_COMBINED", "DYNAMICLIGHTMAP_ON", "UNITY_HDR_ON", "SHADOWS_SHADOWMASK", "LIGHTPROBE_SH")))
                .Append(new ShaderLabPragmaCommand("multi_compile_particles", PragmaCommandFlags.None,
                    ShaderLabPragmaInfo.ForImplicitKeywordSetWithDisabledVariant("SOFTPARTICLES_ON")))
                .Append(new ShaderLabPragmaCommand("multi_compile_fog", PragmaCommandFlags.None,
                    ShaderLabPragmaInfo.ForImplicitKeywordSetWithDisabledVariant("FOG_LINEAR", "FOG_EXP", "FOG_EXP2")))
                .Append(new ShaderLabPragmaCommand("multi_compile_instancing", PragmaCommandFlags.None,
                    ShaderLabPragmaInfo.ForImplicitKeywordSetWithDisabledVariant("INSTANCING_ON", "PROCEDURAL_ON")))
                .Append(EnableD3D11DebugSymbols).Append(HlslccBytecodeDisassembly)
                .Append(OnlyRenderers).Append(ExcludeRenderers).Append(Require)
                .Append(SkipOptimizations).Append(DisableFastmath)
                .Concat(PragmaCommandEx.CreateArrayWithFlags(PragmaCommandFlags.HasRequiredSpec, "target", "hardware_tier_variants", "skip_variants", "instancing_options"))
                .Append(DynamicBranch).Append(DynamicBranchLocal)
                .Append(MultiCompile).Append(MultiCompileLocal).Concat(WithStageSuffixes(MultiCompile)).Concat(WithStageSuffixes(MultiCompileLocal))
                .Append(ShaderFeature).Append(ShaderFeatureLocal).Concat(WithStageSuffixes(ShaderFeature)).Concat(WithStageSuffixes(ShaderFeatureLocal))
                .Append(new ShaderLabPragmaCommand("geometry", PragmaCommandFlags.HasRequiredSpec | PragmaCommandFlags.HasFunctionReference, new ShaderLabPragmaInfo { ImpliesShaderTarget = HlslConstants.SHADER_TARGET_40 }))
                .Append(new ShaderLabPragmaCommand("hull", PragmaCommandFlags.HasRequiredSpec | PragmaCommandFlags.HasFunctionReference, new ShaderLabPragmaInfo { ImpliesShaderTarget = HlslConstants.SHADER_TARGET_46 }))
                .Append(new ShaderLabPragmaCommand("domain", PragmaCommandFlags.HasRequiredSpec | PragmaCommandFlags.HasFunctionReference, new ShaderLabPragmaInfo { ImpliesShaderTarget = HlslConstants.SHADER_TARGET_46 }))
                .Concat(PragmaCommandEx.CreateArrayWithFlags(PragmaCommandFlags.HasRequiredSpec | PragmaCommandFlags.HasFunctionReference, "vertex", "fragment", "surface"))
        );

        public UnityHlslPragmas(bool includeHlslStandardPragmas) : 
            base(ourUnityCommands.Concat(CppLanguageDialectBase.RegionPragmaCommands).Concat(includeHlslStandardPragmas ? CppHLSLDialect.HlslPragmaCommands : EmptyList<PragmaCommand>.Enumerable))
        {
        }

        private static IEnumerable<PragmaCommand> WithStageSuffixes(PragmaCommand command)
        {
            yield return command.WithSuffix("_vertex");
            yield return command.WithSuffix("_fragment");
            yield return command.WithSuffix("_hull");
            yield return command.WithSuffix("_domain");
            yield return command.WithSuffix("_geometry");
            yield return command.WithSuffix("_raytracing");
        }
    }
}