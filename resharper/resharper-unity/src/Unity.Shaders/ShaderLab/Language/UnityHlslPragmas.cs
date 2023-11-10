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
        public static readonly ShaderLabPragmaCommand MultiCompile = new("multi_compile", PragmaCommandFlags.HasRequiredSpec, new ShaderLabPragmaInfo { DeclaresKeywords = true });
        public static readonly PragmaCommand MultiCompileLocal = MultiCompile.WithSuffix("_local");
        public static readonly ShaderLabPragmaCommand ShaderFeature = new("shader_feature", PragmaCommandFlags.HasRequiredSpec, new ShaderLabPragmaInfo { DeclaresKeywords = true, HasDisabledVariantForSingleKeyword = true });
        public static readonly PragmaCommand ShaderFeatureLocal = ShaderFeature.WithSuffix("_local");
        public static readonly ShaderLabPragmaCommand DynamicBranch = new("dynamic_branch", PragmaCommandFlags.HasRequiredSpec, new ShaderLabPragmaInfo { DeclaresKeywords = true });
        public static readonly PragmaCommand DynamicBranchLocal = DynamicBranch.WithSuffix("_local");
        public static readonly PragmaCommand EnableD3D11DebugSymbols = new("enable_d3d11_debug_symbols", PragmaCommandFlags.None);
        public static readonly PragmaCommand OnlyRenderers = new("only_renderers", PragmaCommandFlags.HasRequiredSpec);
        public static readonly PragmaCommand ExcludeRenderers = new("exclude_renderers", PragmaCommandFlags.HasRequiredSpec);
        public static readonly PragmaCommand Require = new("require", PragmaCommandFlags.HasRequiredSpec);
        public static readonly PragmaCommand SkipOptimizations = new("skip_optimizations", PragmaCommandFlags.HasRequiredSpec);
        public static readonly PragmaCommand DisableFastmath = new("disable_fastmath", PragmaCommandFlags.None);
        public static readonly PragmaCommand HlslccBytecodeDisassembly = new("hlslcc_bytecode_disassembly", PragmaCommandFlags.None);

        private static readonly ImmutableArray<PragmaCommand> ourUnityCommands = ImmutableArray.CreateRange(
            Enumerable.Concat(PragmaCommandEx.CreateArrayWithFlags(PragmaCommandFlags.None,
                        "editor_sync_compilation", "enable_cbuffer",
                        "mutli_compile_fwdbase", "multi_compile_fwdbasealpha", "multi_compile_fwdadd",
                        "multi_compile_fwdadd_fullshadows", "multi_compile_lightpass", "multi_compile_shadowcaster",
                        "multi_compile_shadowcollector", "multi_compile_prepassfinal", "multi_compile_particles",
                        "multi_compile_fog", "multi_compile_instancing"
                    )
                    .Append(EnableD3D11DebugSymbols).Append(HlslccBytecodeDisassembly)
                    .Append(OnlyRenderers).Append(ExcludeRenderers).Append(Require)
                    .Append(SkipOptimizations).Append(DisableFastmath), PragmaCommandEx.CreateArrayWithFlags(PragmaCommandFlags.HasRequiredSpec,
                    "target", "hardware_tier_variants", "skip_variants", "instancing_options"))
                .Append(DynamicBranch).Append(DynamicBranchLocal)
                .Append(MultiCompile).Append(MultiCompileLocal).Concat(WithStageSuffixes(MultiCompile))
                .Append(ShaderFeature).Append(ShaderFeatureLocal).Concat(WithStageSuffixes(ShaderFeature))
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