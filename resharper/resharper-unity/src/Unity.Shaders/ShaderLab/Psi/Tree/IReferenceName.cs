using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Resolve;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree
{
    public partial interface IReferenceName
    {
        IVariableReferenceReference Reference { get; }
    }
}