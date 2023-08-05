using JetBrains.Application.Progress;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Parsing;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CodeStyle;
using JetBrains.ReSharper.Psi.Cpp.Language;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Format;
using JetBrains.ReSharper.Psi.Impl.CodeStyle;
using JetBrains.ReSharper.Psi.Impl.Shared.InjectedPsi;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util.Text;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Formatting
{
  [Language(typeof(ShaderLabLanguage))]
  public class ShaderLabCodeFormatter : CodeFormatterBase<ShaderLabFormatSettingsKey>
  {
    private readonly ShaderLabFormattingInfoProvider myShaderLabFormattingInfo;

    public ShaderLabCodeFormatter(PsiLanguageType languageType, CodeFormatterRequirements requirements, ShaderLabFormattingInfoProvider shaderLabFormattingInfo) : base(languageType, requirements)
    {
      myShaderLabFormattingInfo = shaderLabFormattingInfo;
    }
    
    public override string OverridenSettingPrefix => "// @formatter:";

    protected override CodeFormattingContext CreateFormatterContext(
        AdditionalFormatterParameters parameters, ICustomFormatterInfoProvider provider, int tabWidth, SingleLangChangeAccu changeAccu, FormatTask[] formatTasks)
    {
      return new CodeFormattingContext(this, FormatterLoggerProvider.FormatterLogger, parameters, tabWidth, changeAccu, formatTasks);
    }

    public override MinimalSeparatorType GetMinimalSeparatorByNodeTypes(TokenNodeType leftToken, TokenNodeType rightToken)
    {
      return MinimalSeparatorType.NotRequired;
    }

    public override ITreeNode CreateSpace(string indent, NodeType replacedOrLeftSiblingType)
    {
      return ShaderLabTokenType.WHITESPACE.CreateLeafElement(indent);
    }

    public override ITreeNode CreateNewLine(LineEnding lineEnding, NodeType lineBreakType = null)
    {
      return ShaderLabTokenType.NEW_LINE.CreateLeafElement(lineEnding.GetPresentation());
    }

    public override ITreeRange Format(ITreeNode firstElement, ITreeNode lastElement, CodeFormatProfile profile,
      AdditionalFormatterParameters parameters = null)
    {
      parameters = parameters ?? AdditionalFormatterParameters.Empty;
      var pointer = FormatterImplHelper.CreateRangePointer(firstElement, lastElement);
      ITreeNode lastNode = lastElement;
      
      var task = new FormatTask(firstElement, lastNode, profile);
      task.Adjust(this);
      if (task.FirstElement == null) return new TreeRange(firstElement, lastElement);

      //ASSERT(!IsWhitespaceToken(lastNode), "Whitespace node on the right side of the range");

      var settings = GetFormattingSettings(task.FirstElement, parameters, myShaderLabFormattingInfo);
      settings.Settings.SetValue((key => key.WRAP_LINES), false);

      // TODO: this is a hack to warmup injected CPP nodes in PSI to avoid problems with parsing under write lock during formatting, we need more robust solution for that problem 
      if (firstElement.GetSourceFile() is {} sourceFile)
        _ = sourceFile.GetPsiFiles<CppLanguage>().Count;
      DoDeclarativeFormat(settings, myShaderLabFormattingInfo, null, new[] { task }, parameters,
        null, FormatChildren);

      return FormatterImplHelper.PointerToRange(pointer, firstElement, lastElement);
      
      void FormatChildren(FormatTask formatTask, FmtSettingsHolder<ShaderLabFormatSettingsKey> formatSettings, CodeFormattingContext context, IProgressIndicator pi)
      {
          using var fmtProgress = pi.CreateSubProgress(1);
          
          Assertion.Assert(formatTask.FirstElement != null, "firstNode != null");
          var file = formatTask.FirstElement.GetContainingFile();
          if (file != null)
          {
              if (ShaderLabDoNotFormatInjectionsCookie.IsInjectionFormatterSuppressed)
                  return;
              
              using (new SuspendInjectRegenerationCookie())
              {
                  FormatterImplHelper.RunFormatterForGeneratedLanguages(file, formatTask.FirstElement, lastNode, profile,
                      it => true, PsiLanguageCategories.All, parameters.ChangeProgressIndicator(fmtProgress));
              }
          }
      }
    }



    public override void FormatInsertedNodes(ITreeNode nodeFirst, ITreeNode nodeLast, bool formatSurround,
        bool indentSurround = false)
    {
      
    }

    public override ITreeRange FormatInsertedRange(ITreeNode nodeFirst, ITreeNode nodeLast, ITreeRange origin)
    {
      return new TreeRange(nodeFirst, nodeLast);
    }

    public override void FormatReplacedNode(ITreeNode oldNode, ITreeNode newNode)
    {
    }

    public override void FormatReplacedRange(ITreeNode first, ITreeNode last, ITreeRange oldNodes)
    {
    }

    public override void FormatDeletedNodes(ITreeNode parent, ITreeNode prevNode, ITreeNode nextNode)
    {
    }
  }
}