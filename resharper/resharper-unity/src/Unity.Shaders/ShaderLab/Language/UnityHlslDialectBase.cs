#nullable enable
using System.Collections.Immutable;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Core.Semantic;
using JetBrains.ReSharper.Psi.Cpp.Language;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Language
{
    public abstract class UnityHlslDialectBase : CppHLSLDialect
    {
        public abstract ImmutableArray<HlslSemantic> Semantics { get; }
        
        protected UnityHlslDialectBase() : base(true)
        {
        }
    }
}