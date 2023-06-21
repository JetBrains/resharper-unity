#nullable enable
using System.Collections.Generic;
using JetBrains.ReSharper.Psi.Cpp.Language;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Language
{
    public sealed class UnityShaderLabHlslDialect : CppHLSLDialect
    {
        private static readonly UnityHlslPragmas ourPragmas = new(false);

        public override IReadOnlyDictionary<string, PragmaCommand> Pragmas => ourPragmas;

        public UnityShaderLabHlslDialect() : base(true)
        {
        }
    }
}