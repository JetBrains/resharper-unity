using JetBrains.ReSharper.Plugins.Yaml.Psi.Parsing;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Text;

namespace JetBrains.ReSharper.Plugins.Yaml.Psi.Tree.Impl
{
  internal abstract class WhitespaceBase : YamlTokenBase, IWhitespaceNode
  {
    protected WhitespaceBase(IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
      : base(buffer, startOffset, endOffset)
    {
    }

    public abstract bool IsNewLine { get; }

    public override bool IsFiltered() => true;

    public override string ToString()
    {
      return base.ToString() + " spaces: \"" + GetText() + "\"";
    }
  }

  internal class Whitespace : WhitespaceBase
  {
    public Whitespace(IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
      : base(buffer, startOffset, endOffset)
    {
    }

    public override NodeType NodeType => YamlTokenType.WHITESPACE;
    public override bool IsNewLine => false;
  }

  internal class NewLine : WhitespaceBase
  {
    public NewLine(IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
      : base(buffer, startOffset, endOffset)
    {
    }

    public override NodeType NodeType => YamlTokenType.NEW_LINE;
    public override bool IsNewLine => true;
  }
}