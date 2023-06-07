using JetBrains.Application.Settings;
using JetBrains.Application.Settings.Calculated.Interface;
using JetBrains.Application.Threading;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree.Impl;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Parsing;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Impl.CodeStyle;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Formatting
{
  [Language(typeof(ShaderLabLanguage))]
  public class ShaderLabFormattingInfoProvider : FormatterInfoProviderWithFluentApi<CodeFormattingContext, ShaderLabFormatSettingsKey>
  {
    public ShaderLabFormattingInfoProvider(ISettingsSchema settingsSchema, ICalculatedSettingsSchema calculatedSettingsSchema, IThreading threading, Lifetime lifetime) : base(settingsSchema, calculatedSettingsSchema, threading, lifetime)
    {
    }

    protected override void Initialize()
    {
      base.Initialize();
      var bracesRule = Describe<BracesRule>()
        .LPar(ShaderLabTokenType.LBRACE)
        .RPar(ShaderLabTokenType.RBRACE)
        // .EmptyBlockSetting(it => it.EMPTY_BLOCK_STYLE)
        //.SpacesInsideParsSetting(it => it.SPACE_WITHING_EMPTY_BRACES)
        // .SpacesInsideEmptyParsSetting(it => it.SPACE_WITHING_EMPTY_BRACES)
        .ProhibitBlankLinesNearBracesInBsdStyle(false)
        // .MaxBlankLinesBeforeLBraceSetting(it => it.KEEP_BLANK_LINES_IN_DECLARATIONS)
        // .RemoveBlankLinesInsideBracesSetting(it => it.REMOVE_BLANK_LINES_NEAR_BRACES_IN_DECLARATIONS)
        // .MaxBlankLinesInsideSetting(it => it.KEEP_BLANK_LINES_IN_DECLARATIONS)
        // .LineBreaksForEmptyParsHighlighting(CSharpFormatHighlightingIds.BadEmptyBracesLineBreaks)
        // .ParsBlankLinesHighlighting(CSharpFormatHighlightingIds.IncorrectBlankLinesNearBraces)
        // .ParsSpacesHighlighting(CSharpFormatHighlightingIds.BadBracesSpaces)
        //.Priority(2)
        .StartAlternating();
      
      bracesRule.Name("TYPE_DECLARATION_BRACES")
        .Where(
          Node().In(ElementType.BIND_CHANNELS_VALUE, ElementType.CATEGORY_VALUE, ElementType.FOG_VALUE, 
            ElementType.GRAB_PASS_VALUE, ElementType.MATERIAL_VALUE, ElementType.PROPERTIES_VALUE,
            ElementType.SET_TEXTURE_VALUE, ElementType.STENCIL_VALUE,
            ElementType.SUB_SHADER_VALUE, ElementType.TAGS_VALUE, ElementType.TEXTURE_PASS_VALUE, ElementType.TEXTURE_PROPERTY_VALUE, ElementType.PACKAGE_REQUIREMENTS_VALUE))
        .BraceSetting(it => it.BraceStyle)
        // .InsertBlankLinesInsideBracesSetting(it => it.BLANK_LINES_INSIDE_TYPE)
        .Priority(2)
        // .ParsIndentingHighlighting(CSharpFormatHighlightingIds.BadDeclarationBracesIndent)
        // .ParsLineBreaksHighlighting(CSharpFormatHighlightingIds.BadDeclarationBracesLineBreaks)
        .Build();

      bracesRule.Name("TYPE_DECLARATION_BRACES")
        .Where(
          Parent().In(ElementType.SHADER_VALUE))
        .BraceSetting(it => it.BraceStyle)
        // .InsertBlankLinesInsideBracesSetting(it => it.BLANK_LINES_INSIDE_TYPE)
        .Priority(2)
        // .ParsIndentingHighlighting(CSharpFormatHighlightingIds.BadDeclarationBracesIndent)
        // .ParsLineBreaksHighlighting(CSharpFormatHighlightingIds.BadDeclarationBracesLineBreaks)
        .Build();

      Describe<FormattingRule>()
        .Name("Disable Whitespaces in CGProgram")
        .Group(FormatterInfoProviderBase.AllRuleGroup)
        .Priority(1000)
        .Where(
          Parent().In(ElementType.CG_PROGRAM_BLOCK, ElementType.CG_INCLUDE_BLOCK,
            ElementType.HLSL_INCLUDE_BLOCK, ElementType.HLSL_PROGRAM_BLOCK,
            ElementType.GLSL_INCLUDE_BLOCK, ElementType.GLSL_PROGRAM_BLOCK))
        .Return(IntervalFormatType.ReallyDoNotChangeAnything);
    }

    public override ProjectFileType MainProjectFileType => ShaderLabProjectFileType.Instance;
  }
}