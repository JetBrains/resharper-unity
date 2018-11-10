using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Parsing;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Yaml.Psi.Tree.Impl
{
  public class Comment : YamlTokenBase, ICommentNode
  {
    [NotNull] private readonly string myText;

    public Comment([NotNull] string text)
    {
      myText = text;
    }

    public override int GetTextLength() => myText.Length;
    public override string GetText() => myText;
    public override bool IsFiltered() => true;
    public override NodeType NodeType => YamlTokenType.COMMENT;
    public string CommentText => GetText().Substring(1);

    public TreeTextRange GetCommentRange()
    {
      var treeStartOffset = GetTreeStartOffset();
      return new TreeTextRange(treeStartOffset + 2, treeStartOffset + GetTextLength());
    }

    public override string ToString()
    {
      return base.ToString() + " spaces: " + "\"" + GetText() + "\"";
    }
  }
}