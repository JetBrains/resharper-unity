#nullable enable
using System.Collections.Immutable;
using JetBrains.ReSharper.Psi.Cpp.Types;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Core.Semantic
{
    public class HlslSemantic
    {
        public readonly string Name;
        public readonly HlslSemanticScope Scope;
        public readonly ImmutableArray<CppQualType> SupportedTypes;

        public HlslSemantic(string name, HlslSemanticScope scope, params CppQualType[] supportedTypes)
        {
            Name = name;
            Scope = scope;
            SupportedTypes = ImmutableArray.Create(supportedTypes);
        }

        public bool IsTypeSupported(CppQualType type) => SupportedTypes.IndexOf(type, 0,  CppQualTypeUnresolvedComparer.INSTANCE) >= 0;
    }
}