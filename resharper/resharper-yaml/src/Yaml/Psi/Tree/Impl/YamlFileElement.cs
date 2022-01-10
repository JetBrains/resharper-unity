using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;

namespace JetBrains.ReSharper.Plugins.Yaml.Psi.Tree.Impl
{
  public abstract class YamlFileElement : FileElementBase
  {
    // ReSharper disable once AssignNullToNotNullAttribute
    public override PsiLanguageType Language => YamlLanguage.Instance;

    public abstract void Accept(TreeNodeVisitor visitor);
    public abstract void Accept<TContext>(TreeNodeVisitor<TContext> visitor, TContext context);
    public abstract TReturn Accept<TContext, TReturn>(TreeNodeVisitor<TContext, TReturn> visitor, TContext context);
  }
}