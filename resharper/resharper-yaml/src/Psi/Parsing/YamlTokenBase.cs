using System;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Text;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Yaml.Psi.Parsing
{
  public abstract class YamlTokenBase : BoundToBufferLeafElement, IYamlTreeNode, ITokenNode
  {
    protected YamlTokenBase(NodeType nodeType, IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
      : base(nodeType, buffer, startOffset, endOffset)
    {
    }

    public override PsiLanguageType Language => LanguageFromParent;

    public virtual void Accept(TreeNodeVisitor visitor)
    {
      visitor.VisitNode(this);
    }

    public virtual void Accept<TContext>(TreeNodeVisitor<TContext> visitor, TContext context)
    {
      visitor.VisitNode(this, context);
    }

    public virtual TResult Accept<TContext, TResult>(TreeNodeVisitor<TContext, TResult> visitor, TContext context)
    {
      return visitor.VisitNode(this, context);
    }

    public TokenNodeType GetTokenType() => (TokenNodeType) NodeType;

    public override string ToString()
    {
      var text = GetTextAsBuffer().GetText(new TextRange(0, Math.Min(100, Length)));
      return $"{base.ToString()}(type:{NodeType}, text:{text})";
    }
  }
}