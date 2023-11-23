#nullable enable
using System.Collections.Immutable;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.ShaderVariants;

public interface IShaderDefineSymbolDescriptor
{
    ImmutableArray<string> AllSymbols { get; }
    bool IsDefaultSymbol(string defineSymbol);
}