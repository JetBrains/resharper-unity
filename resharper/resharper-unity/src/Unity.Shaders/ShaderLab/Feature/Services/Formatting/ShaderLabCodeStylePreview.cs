using System;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.OptionPages.CodeStyle;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Parsing;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Feature.Services.Formatting
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

        case PreviewParseType.Statement:
          return null;  // TODO (DK) to be implemented

        default:
          throw new NotImplementedException($"PreviewParseType = {parseType}");
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