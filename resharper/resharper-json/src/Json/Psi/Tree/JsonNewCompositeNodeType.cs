using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Tree
{
    public abstract class JsonNewCompositeNodeType : CompositeNodeType
    {
        protected JsonNewCompositeNodeType(string s, int index)
            : base(s, index)
        {
        }
    }
}