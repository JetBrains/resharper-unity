using JetBrains.ReSharper.Plugins.Unity.Common.Services.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree.Impl
{
    internal partial class SubShaderCommand
    {
        protected override void CollectCustomChildDeclarations(ITreeNode child, ref LocalList<IStructuralDeclaration> declarations)
        {
            if (child is not IPass pass) return;
            foreach (var passChild in pass.Children())
            {
                if (passChild is IStructuralDeclaration passChildDeclaration)
                    declarations.Add(passChildDeclaration);
                else if (passChild is ITexturePassDeclaration { Command: { } command })
                    declarations.Add(command);
            }
        }
    }
}
