using System.Collections.Generic;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree.Impl
{
    internal partial class ShaderCommand
    {
        public override IEnumerable<IHierarchicalDeclaration> GetChildDeclarations()
        {
            if (Value is not IShaderValue shaderValue)
                yield break;
            if (shaderValue.PropertiesCommand is IHierarchicalDeclaration propertiesCommand)
                yield return propertiesCommand;
            foreach (var shaderBlock in shaderValue.ShaderBlocks)
            {
                if (shaderBlock.FirstChild is IHierarchicalDeclaration shaderBlockCommand)
                    yield return shaderBlockCommand;
            }
        }
    }
}