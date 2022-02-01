namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.DeclaredElements
{
    public interface IPropertyDeclaredElement : IShaderLabDeclaredElement
    {
        string GetDisplayName();
        string GetPropertyType();
    }
}