using System;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.OptionPages.CodeStyle;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Parsing;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Feature.Services.Formatting
{
  [CodePreviewPreparatorComponent]
  public class ShaderLabCodeStylePreview : CodePreviewPreparator
  {
    protected override ITreeNode Parse(IParser parser, PreviewParseType parseType)
    {
      var shaderLabParser = (IShaderLabParser)parser;
      switch (parseType)
      {
        case PreviewParseType.File:
          return shaderLabParser.ParseFile();

        case PreviewParseType.None:
          return null;

        default:
          throw new NotImplementedException();
      }
    }

    public override KnownLanguage Language
    {
      get { return ShaderLabLanguage.Instance; }
    }

    public override ProjectFileType ProjectFileType
    {
      get { return ShaderLabProjectFileType.Instance; }
    }
  }
}