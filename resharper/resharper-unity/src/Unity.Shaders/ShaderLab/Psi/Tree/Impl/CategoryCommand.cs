#nullable enable
using JetBrains.ReSharper.Plugins.Unity.Common.Services.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree.Impl
{
    internal partial class CategoryCommand
    {
        protected override void CollectCustomChildDeclarations(ITreeNode child, ref LocalList<IStructuralDeclaration> declarations)
        {
            if (child is IShaderBlock { FirstChild: IStructuralDeclaration shaderBlockCommand })
                declarations.Add(shaderBlockCommand);
        }

        public override ITokenNode? GetEntityNameToken() => (Value as ICategoryValue)?.StateCommandsEnumerable.LastOrDefaultOfType<IStateCommand, INameCommand>()?.Name;
    }
}