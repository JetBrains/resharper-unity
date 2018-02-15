using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Tree;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.DeclaredElements
{
    public class PropertyDeclaredElement : ShaderLabDeclaredElementBase, IPropertyDeclaredElement
    {
        public PropertyDeclaredElement(string shortName, IPsiSourceFile sourceFile, int treeOffset)
            : base(shortName, sourceFile, treeOffset)
        {
        }

        public override DeclaredElementType GetElementType() => ShaderLabDeclaredElementType.Property;

        public string GetDisplayName()
        {
            var declarations = GetDeclarations();
            if (declarations.Count > 0 && declarations[0] is IPropertyDeclaration declaration)
                return declaration.DisplayName?.GetText() ?? string.Empty;
            return string.Empty;
        }

        public string GetPropertyType()
        {
            var declarations = GetDeclarations();
            if (declarations.Count > 0 && declarations[0] is IPropertyDeclaration declaration)
                return declaration.PropertyType?.GetText() ?? string.Empty;
            return string.Empty;
        }
    }
}