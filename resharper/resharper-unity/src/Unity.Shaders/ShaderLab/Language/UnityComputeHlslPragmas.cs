#nullable enable
using JetBrains.ReSharper.Psi.Cpp.Language;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Language
{
    public sealed class UnityComputeHlslPragmas : CppPragmas
    {
        public static readonly PragmaCommand Kernel = new("kernel", PragmaCommandFlags.HasRequiredSpec | PragmaCommandFlags.HasFunctionReference);
        
        public UnityComputeHlslPragmas() : base(new[]
        {
            Kernel, CppLanguageDialectBase.WarningPragmaCommand, UnityHlslPragmas.MultiCompile, UnityHlslPragmas.MultiCompileLocal, UnityHlslPragmas.EnableD3D11DebugSymbols,
            UnityHlslPragmas.Require, UnityHlslPragmas.ExcludeRenderers, UnityHlslPragmas.OnlyRenderers, UnityHlslPragmas.HlslccBytecodeDisassembly, UnityHlslPragmas.DisableFastmath
        })
        {
        }
    }
}