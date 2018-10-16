namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.DeclaredElements
{
    public interface IPropertyDeclaredElement : IShaderLabDeclaredElement
    {
        string GetDisplayName();
        string GetPropertyType();
    }
}