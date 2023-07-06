#nullable enable
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Common.Psi.Resolve;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Caches;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Resolve
{
    public class ShaderReference<TOwner> : ReferenceWithOrigin<TOwner>, IShaderReference where TOwner : ITreeNode
    {
        public ShaderReference(TOwner owner, IReferenceOrigin<TOwner> origin) : base(owner, origin)
        {
        }

        protected override ISymbolTable GetLookupSymbolTable() => myOwner.GetSolution().GetComponent<ShaderLabCache>().GetShaderSymbolTable();

        protected override ResolveResultWithInfo ResolveByName(string name) => new(EmptyResolveResult.Instance, ShaderLabResolveErrorType.SHADERLAB_SHADER_REFERENCE_UNRESOLVED_WARNING);
    }
}