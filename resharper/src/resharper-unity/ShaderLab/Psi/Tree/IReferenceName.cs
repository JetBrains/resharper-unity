using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Resolve;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Tree
{
    public partial interface IReferenceName
    {
        IVariableReferenceReference Reference { get; }
    }
}