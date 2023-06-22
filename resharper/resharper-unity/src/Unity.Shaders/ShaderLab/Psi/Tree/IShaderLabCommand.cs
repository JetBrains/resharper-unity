#nullable enable
using JetBrains.ReSharper.Plugins.Unity.Common.Services.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree
{
    public partial interface IShaderLabCommand : IStructuralDeclaration
    {
        ITokenNode? GetEntityNameToken();
    }
}