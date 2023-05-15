using System.Collections.Generic;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree.Impl
{
    internal partial class SubShaderCommand
    {
        public override IEnumerable<IHierarchicalDeclaration> GetChildDeclarations()
        {
            if (Value is not ISubShaderValue subShaderValue)
                yield break;
            foreach (var pass in subShaderValue.Passes)
            {
                foreach (var stateCommand in pass.StateCommands)
                {
                    if (stateCommand is IHierarchicalDeclaration declaration)
                        yield return declaration;
                }

                if (pass.PassDefinition is IHierarchicalDeclaration passDeclaration)
                    yield return passDeclaration;
            }
        }
    }
}
