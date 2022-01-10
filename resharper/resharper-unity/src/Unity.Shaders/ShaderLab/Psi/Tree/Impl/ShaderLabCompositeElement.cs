using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Tree.Impl
{
    public abstract class ShaderLabCompositeElement : CompositeElement, IShaderLabTreeNode
    {
        // ReSharper disable once AssignNullToNotNullAttribute
        public override PsiLanguageType Language => ShaderLabLanguage.Instance;

        public virtual void Accept(TreeNodeVisitor visitor)
        {
            visitor.VisitNode(this);
        }

        public virtual void Accept<TContext>(TreeNodeVisitor<TContext> visitor, TContext context)
        {
            visitor.VisitNode(this, context);
        }

        public virtual TReturn Accept<TContext, TReturn>(TreeNodeVisitor<TContext, TReturn> visitor, TContext context)
        {
            return visitor.VisitNode(this, context);
        }
    }
}