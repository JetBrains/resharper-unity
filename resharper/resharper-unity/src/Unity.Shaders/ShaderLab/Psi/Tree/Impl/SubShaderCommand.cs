using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.Unity.Services.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree.Impl
{
    internal partial class SubShaderCommand
    {
        public override IEnumerable<IStructuralDeclaration> GetMemberDeclarations()
        {
            if (Value is not ISubShaderValue subShaderValue)
                yield break;
            foreach (var pass in subShaderValue.Passes)
            {
                foreach (var stateCommand in pass.StateCommands)
                {
                    if (stateCommand is IStructuralDeclaration declaration)
                        yield return declaration;
                }

                if (pass.PassDefinition is IStructuralDeclaration passDeclaration)
                    yield return passDeclaration;
            }
        }
    }
}
