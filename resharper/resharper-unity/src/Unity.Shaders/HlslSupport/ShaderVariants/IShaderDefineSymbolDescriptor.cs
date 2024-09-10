#nullable enable
using System.Collections.Immutable;
using JetBrains.ReSharper.Psi.Cpp.Caches;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.ShaderVariants;

public interface IShaderDefineSymbolDescriptor
{
    ImmutableArray<string> AllSymbols { get; }
    bool IsDefaultSymbol(string defineSymbol);
    bool IsApplicable(CppFileLocation location);
}