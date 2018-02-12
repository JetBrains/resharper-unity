using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.DeclaredElements
{
    public class PropertyDeclaredElement : ShaderLabDeclaredElementBase
    {
        public PropertyDeclaredElement(string shortName, IPsiSourceFile sourceFile, int treeOffset)
            : base(shortName, sourceFile, treeOffset)
        {
        }

        public override DeclaredElementType GetElementType() => ShaderLabDeclaredElementType.Property;
    }
}