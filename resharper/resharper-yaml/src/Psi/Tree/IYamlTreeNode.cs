using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Yaml.Psi.Tree
{
  public interface IYamlTreeNode : ITreeNode
  {
    void Accept([NotNull] TreeNodeVisitor visitor);
    void Accept<TContext>([NotNull] TreeNodeVisitor<TContext> visitor, TContext context);
    TReturn Accept<TContext, TReturn>([NotNull] TreeNodeVisitor<TContext, TReturn> visitor, TContext context);
  }
}