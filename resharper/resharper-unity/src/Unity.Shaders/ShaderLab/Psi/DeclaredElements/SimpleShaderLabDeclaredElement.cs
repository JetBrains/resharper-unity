#nullable enable
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.DeclaredElements
{
    public class SimpleShaderLabDeclaredElement : ShaderLabDeclaredElementBase
    {
        private readonly DeclaredElementType myElementType; 
        
        public SimpleShaderLabDeclaredElement(string shortName, IPsiSourceFile sourceFile, int treeOffset, DeclaredElementType elementType) : base(shortName, sourceFile, treeOffset)
        {
            myElementType = elementType;
        }

        public override DeclaredElementType GetElementType() => myElementType;

        public override bool Equals(object? obj) => base.Equals(obj) && myElementType == ((SimpleShaderLabDeclaredElement)obj).myElementType; 

        public override int GetHashCode() => base.GetHashCode(); // we are fine with base hash code, because in most cases we don't have two declared elements at same location with different element type
    }
}