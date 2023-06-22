#nullable enable
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Common.Psi.Resolve;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Caches;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Resolve
{
    public class ShaderReference<TOwner> : ReferenceWithOrigin<TOwner> where TOwner : ITreeNode
    {
        public ShaderReference(TOwner owner, IReferenceOrigin<TOwner> origin) : base(owner, origin)
        {
        }

        protected override ISymbolTable GetLookupSymbolTable() => myOwner.GetSolution().GetComponent<ShaderLabCache>().GetShaderSymbolTable();
    }
}