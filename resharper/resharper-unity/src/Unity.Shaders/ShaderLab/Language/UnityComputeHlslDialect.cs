#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Core.Semantic;
using JetBrains.ReSharper.Psi.Cpp.Language;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Language
{
    public sealed class UnityComputeHlslDialect : UnityHlslDialectBase
    {
        private static readonly UnityComputeHlslPragmas ourPragmas = new();

        public override IReadOnlyDictionary<string, PragmaCommand> Pragmas => ourPragmas;
        public override ImmutableArray<HlslSemantic> Semantics => ImmutableArray<HlslSemantic>.Empty;
    }
}