using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Tree
{
    public abstract class ShaderLabCompositeNodeType : CompositeNodeType
    {
        protected ShaderLabCompositeNodeType(string s, int index)
            : base(s, index)
        {
        }
    }
}