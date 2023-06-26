#nullable enable
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.DeclaredElements
{
    public sealed class ShaderLabCommandDeclaredElement : ShaderLabDeclaredElementBase, IShaderLabCommandDeclaredElement
    {
        public string? EntityName { get; } 
        
        public ShaderLabCommandDeclaredElement(string shortName, string? entityName, IPsiSourceFile sourceFile, int treeOffset) : base(shortName, sourceFile, treeOffset)
        {
            EntityName = entityName;
        }
        
        public override DeclaredElementType GetElementType() => ShaderLabDeclaredElementType.Command;
    }
}