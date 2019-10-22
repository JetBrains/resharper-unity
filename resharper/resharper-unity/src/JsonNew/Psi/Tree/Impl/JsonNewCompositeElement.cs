using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Language;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Tree.Impl
{
    public abstract class JsonNewCompositeElement : CompositeElement, IJsonNewTreeNode
    {
        // ReSharper disable once AssignNullToNotNullAttribute
        public override PsiLanguageType Language => JsonNewLanguage.Instance;

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