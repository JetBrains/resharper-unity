#nullable enable

using System.Collections.Generic;
using JetBrains.ReSharper.Psi.Cpp.Language;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Language
{
    public sealed class UnityHlslDialect : CppHLSLDialect
    {
        private static readonly UnityHlslPragmas ourPragmas = new(true);

        public override IReadOnlyDictionary<string, PragmaCommand> Pragmas => ourPragmas;
        
        public UnityHlslDialect() : base(true) { }
    }
}