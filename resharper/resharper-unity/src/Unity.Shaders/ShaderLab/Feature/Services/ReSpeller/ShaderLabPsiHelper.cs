using System.Collections.Generic;
using JetBrains.ReSharper.Features.ReSpeller.Analyzers;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Parsing;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Feature.Services.ReSpeller
{
  [Language(typeof(ShaderLabLanguage))]
  public class ShaderLabPsiHelper : PsiHelperBase {
    public override bool IsMultiLineComment(IComment comment)
    {
      return comment.NodeType == ShaderLabTokenType.MULTI_LINE_COMMENT;
    }

    public override ITreeNode[] GetConcatenatedLiterals(ITreeNode literal)
    {
      var node = literal.Parent;
      var result = new List<ITreeNode>();
      if (TryCollectLiterals(node, result))
        return result.ToArray();
      return base.GetConcatenatedLiterals(literal);
    }

    private bool TryCollectLiterals(ITreeNode expression, List<ITreeNode> literals)
    {
      switch (expression)
      {
        case ILiteralExpression:
          literals.Add(expression);
          return true;
        default:
          return false;
      }
    }
  }
}