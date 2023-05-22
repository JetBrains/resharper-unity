using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.Unity.Services.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree.Impl
{
    internal partial class ShaderCommand
    {
        public override IEnumerable<IStructuralDeclaration> GetMemberDeclarations()
        {
            if (Value is not IShaderValue shaderValue)
                yield break;
            if (shaderValue.PropertiesCommand is IStructuralDeclaration propertiesCommand)
                yield return propertiesCommand;
            foreach (var shaderBlock in shaderValue.ShaderBlocks)
            {
                if (shaderBlock.FirstChild is IStructuralDeclaration shaderBlockCommand)
                    yield return shaderBlockCommand;
            }
        }
    }
}