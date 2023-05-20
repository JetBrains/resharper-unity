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
    }
}