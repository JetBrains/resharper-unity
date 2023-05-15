using System.Collections.Generic;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree.Impl
{
    internal partial class CategoryCommand
    {
        public override IEnumerable<IHierarchicalDeclaration> GetChildDeclarations()
        {
            if (Value is not ICategoryValue categoryValue)
                yield break;
            foreach (var stateCommand in categoryValue.StateCommands)
            {
                if (stateCommand is IHierarchicalDeclaration declaration)
                    yield return declaration;
            }
            foreach (var shaderBlock in categoryValue.ShaderBlocks)
            {
                if (shaderBlock.FirstChild is IHierarchicalDeclaration shaderBlockCommand)
                    yield return shaderBlockCommand;
            }
        }
    }
}