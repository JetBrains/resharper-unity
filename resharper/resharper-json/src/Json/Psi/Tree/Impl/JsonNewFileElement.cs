using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;

namespace JetBrains.ReSharper.Plugins.Json.Psi.Tree.Impl
{
    public abstract class JsonNewFileElement : FileElementBase, IJsonNewTreeNode
    {
        public override PsiLanguageType Language => JsonNewLanguage.Instance;

        public abstract void Accept(TreeNodeVisitor visitor);
        public abstract void Accept<TContext>(TreeNodeVisitor<TContext> visitor, TContext context);
        public abstract TReturn Accept<TContext, TReturn>(TreeNodeVisitor<TContext, TReturn> visitor, TContext context);
    }
}