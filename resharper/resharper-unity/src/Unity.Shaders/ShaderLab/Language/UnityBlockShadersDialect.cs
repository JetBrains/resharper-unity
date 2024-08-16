using System.Collections.Immutable;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Core.Semantic;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Language;

public class UnityBlockShadersDialect : UnityHlslDialectBase
{
    public override ImmutableArray<HlslSemantic> Semantics => UnityHlslSemantics.All;
}
