#nullable enable
using JetBrains.ReSharper.Plugins.Unity.Common.Services.Tree;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.DeclaredElements;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree.Impl
{
    internal partial class ShaderCommand
    {
        protected override DeclaredElementType DeclaredElementType => ShaderLabDeclaredElementType.ShaderCommand;

        protected override void CollectCustomChildDeclarations(ITreeNode child, ref LocalList<IStructuralDeclaration> declarations)
        {
            if (child is IShaderBlock { FirstChild: IStructuralDeclaration shaderBlockCommand })
                declarations.Add(shaderBlockCommand);
        }

        public override ITokenNode? GetEntityNameToken() => (Value as IShaderValue)?.Name;
    }
}