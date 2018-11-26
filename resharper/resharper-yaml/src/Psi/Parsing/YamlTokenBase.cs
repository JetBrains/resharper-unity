using System.Text;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Text;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Yaml.Psi.Parsing
{
  public abstract class YamlTokenBase : LeafElementBase, IYamlTreeNode, ITokenNode
  {
    private readonly IBuffer myBuffer;
    private readonly TextRange myRange;

    protected YamlTokenBase(IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
    {
      myBuffer = buffer;
      myRange = new TextRange(startOffset.Offset, endOffset.Offset);
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

    public override StringBuilder GetText(StringBuilder to)
    {
      myBuffer.AppendTextTo(to, myRange);
      return to;
    }

    public override IBuffer GetTextAsBuffer()
    {
      return ProjectedBuffer.Create(myBuffer, myRange);
    }

    public override int GetTextLength() => myRange.Length;
    public override string GetText() => myBuffer.GetText(myRange);

    public override string ToString()
    {
      return base.ToString() + "(type:" + NodeType + ", text:" + GetText() + ")";
    }
  }
}