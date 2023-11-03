#nullable enable
namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.ShaderVariants;

public interface IShaderDefineSymbolDescriptor
{
    bool IsDefaultSymbol(string defineSymbol);
}