#nullable enable
namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.DeclaredElements
{
    public interface IShaderLabCommandDeclaredElement : IShaderLabDeclaredElement
    {
        string? EntityName { get; }
    }
}