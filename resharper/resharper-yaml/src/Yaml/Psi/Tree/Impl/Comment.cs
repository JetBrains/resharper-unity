using JetBrains.ReSharper.Plugins.Yaml.Psi.Parsing;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Text;

namespace JetBrains.ReSharper.Plugins.Yaml.Psi.Tree.Impl
{
  public class Comment : YamlTokenBase, ICommentNode
  {
    public Comment(IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
      : base(YamlTokenType.COMMENT, buffer, startOffset, endOffset)
    {
    }

    public override bool IsFiltered() => true;
    public string CommentText => GetText().Substring(1);

    public TreeTextRange GetCommentRange()
    {
      var treeStartOffset = GetTreeStartOffset();
      return new TreeTextRange(treeStartOffset + 2, treeStartOffset + GetTextLength());
    }

    public override string ToString()
    {
      return base.ToString() + " comment: " + "\"" + GetText() + "\"";
    }
  }
}