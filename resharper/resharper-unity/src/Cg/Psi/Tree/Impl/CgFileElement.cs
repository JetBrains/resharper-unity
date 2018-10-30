using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Tree.Impl
{
    public abstract class CgFileElement : FileElementBase
    {
        // ReSharper disable once AssignNullToNotNullAttribute
        public override PsiLanguageType Language => CgLanguage.Instance;

        public abstract void Accept(TreeNodeVisitor visitor);
        public abstract void Accept<TContext>(TreeNodeVisitor<TContext> visitor, TContext context);
        public abstract TReturn Accept<TContext, TReturn>(TreeNodeVisitor<TContext, TReturn> visitor, TContext context);
    }
}