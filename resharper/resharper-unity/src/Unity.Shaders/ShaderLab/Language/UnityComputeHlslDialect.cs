#nullable enable

using System.Collections.Generic;
using JetBrains.ReSharper.Psi.Cpp.Language;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Language
{
    public sealed class UnityComputeHlslDialect : CppHLSLDialect
    {
        private static readonly UnityComputeHlslPragmas ourPragmas = new();

        public override IReadOnlyDictionary<string, PragmaCommand> Pragmas => ourPragmas;

        public UnityComputeHlslDialect() : base(true) { }
    }
}