#nullable enable
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.DeclaredElements
{
    public sealed class ShaderLabCommandDeclaredElement(string shortName, string? entityName, IPsiSourceFile sourceFile, int treeOffset, DeclaredElementType declaredElementType)
        : ShaderLabDeclaredElementBase(shortName, sourceFile, treeOffset), IShaderLabCommandDeclaredElement
    {
        public string? EntityName { get; } = entityName;

        public override DeclaredElementType GetElementType() => declaredElementType;
    }
}