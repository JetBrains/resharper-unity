#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Core.Semantic;
using JetBrains.ReSharper.Psi.Cpp.Language;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Language
{
    public sealed class UnityHlslDialect : UnityHlslDialectBase
    {
        private static readonly UnityHlslPragmas ourPragmas = new(true);

        public override IReadOnlyDictionary<string, PragmaCommand> Pragmas => ourPragmas;
        public override ImmutableArray<HlslSemantic> Semantics => UnityHlslSemantics.All;
    }
}