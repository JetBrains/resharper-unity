using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Application;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Feature.Services.Cpp.CodeStyle;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Tree;
using JetBrains.ReSharper.Psi.Cpp.Parsing;
using JetBrains.ReSharper.Psi.Cpp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Impl.CodeStyle;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Formatting
{
  [ShellComponent]
  public class ShaderLabCppFormatterExtension : ICppCodeFormatterExtension
  {
    public void AddRules(CppFormattingInfoProvider formattingInfoProvider)
    {
      formattingInfoProvider.RegisterIndentingRules(new ShaderLabIndentingRule());
    }
    
    public class ShaderLabIndentingRule : IIndentingRule
    {
      private readonly INodePattern myNodePattern;
      public string Name => "ShaderLabIndentingRule";

      public ShaderLabIndentingRule()
      {
        myNodePattern = AndNodePattern.Create(new NodeTypePattern(CppCompositeNodeTypes.FILE), new PredicateNodePattern((node, _) => node.Parent is IInjectedFileHolder inject && inject.OriginalNode is ICgContent));
      }

      public long Group { get; set; }
      public int Priority { get; set; }
      public INodePattern NodePattern => myNodePattern;

      public IEnumerable<NodeType> GetNodeTypes() { return NodePattern.GetNodeTypes(); }

      public bool MatchesPatterns(ITreeNode node, CodeFormattingContext context)
      {
        return myNodePattern.Matches(node, context);
      }

      public ITreeNode GetClosingNode(ITreeNode node, CodeFormattingContext checker)
      {
        return node;
      }

      public IOptionNode GetOptionTree(ITreeNode node, CodeFormattingContext context)
      {
        Assertion.Assert(node is CppFile, "node is CppFile");

        var cgProgram = (node.Parent as IInjectedFileHolder)?.OriginalNode.PrevSibling;
        
        var s = GetIndentInCgProgram(cgProgram);
        return new ConstantOptionNode(
          new IndentOptionValue(IndentType.AfterFirstToken | IndentType.AbsoluteIndent | IndentType.NonAdjustable, 0, s));
      }
      

      public string OpeningHighlightingId => null;

      public string ClosingHighlightingId => null;
      public bool IgnoreRegionIfClosingNodeIsNull { get; }
    }
    
    public static string GetIndentInCgProgram(ITreeNode node)
    {
      var indent = new StringBuilder();

      var token = node.GetPreviousToken();
      while (token != null)
      {
        foreach (var c in token.GetText().Reverse())
        {
          switch (c)
          {
            case '\r':
            case '\n':
              return indent.ToString();
            default:
              indent.Append(Char.IsWhiteSpace(c) ? c : ' ');
              break;
          }
        }

        token = token.GetPreviousToken();
      }

      return indent.ToString();
    }

  }
}