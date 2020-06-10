using System;
using JetBrains.Annotations;
using JetBrains.Application.CommandProcessing;
using JetBrains.Application.Environment;
using JetBrains.Application.Environment.Helpers;
using JetBrains.Application.Settings;
using JetBrains.Application.UI.ActionSystem.Text;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.Cpp.TypingAssist;
using JetBrains.ReSharper.Feature.Services.TypingAssist;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CachingLexers;
using JetBrains.ReSharper.Psi.Cpp.Parsing;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Feature.Services.TypingAssists
{
  [SolutionComponent]
  public class ShaderLabTypingAssist : TypingAssistLanguageBase<ShaderLabLanguage>, ITypingHandler
  {
    [NotNull] private readonly ShaderLabDummyFormatter myDummyFormatter;

    public ShaderLabTypingAssist(
      Lifetime lifetime,
      [NotNull] ISolution solution,
      [NotNull] IPsiServices psiServices,
      [NotNull] ICommandProcessor commandProcessor,
      [NotNull] ISettingsStore settingsStore,
      [NotNull] RunsProducts.ProductConfigurations productConfigurations,
      [NotNull] CachingLexerService cachingLexerService,
      [NotNull] ITypingAssistManager typingAssistManager,
      [NotNull] IExternalIntellisenseHost externalIntellisenseHost,
      [NotNull] SkippingTypingAssist skippingTypingAssist,
      [NotNull] LastTypingAssistAction lastTypingAssistAction,
      [NotNull] ShaderLabDummyFormatter dummyFormatter)
      : base(solution, settingsStore, cachingLexerService, commandProcessor, psiServices, externalIntellisenseHost,
        skippingTypingAssist, lastTypingAssistAction)
    {
      myDummyFormatter = dummyFormatter;
      var braceHandler = new ShaderLabBraceHandler(this, dummyFormatter, false, productConfigurations.IsInternalMode());
      var quoteHandler = new CppQuoteHandler<ShaderLabLanguage>(this);
      var deleteHandler = new CppDeleteHandler<ShaderLabLanguage>(this, dummyFormatter);

      typingAssistManager.AddTypingHandler(lifetime, '{', this, c => WrapCppAction(c, braceHandler.HandleLeftBraceTyped),
        IsTypingHandlerAvailable);
      typingAssistManager.AddTypingHandler(lifetime, '}', this, c => WrapCppAction(c, braceHandler.HandleRightBraceTyped),
        IsTypingHandlerAvailable);
      typingAssistManager.AddTypingHandler(lifetime, '(', this, c => WrapCppAction(c, braceHandler.HandleLeftBracketOrParenthTyped),
        IsTypingSmartParenthesisHandlerAvailable);
      typingAssistManager.AddTypingHandler(lifetime, '<', this, c => WrapCppAction(c, braceHandler.HandleLTTyped),
        IsTypingSmartParenthesisHandlerAvailable);
      typingAssistManager.AddTypingHandler(lifetime, '>', this, c => WrapCppAction(c, braceHandler.HandleRightBracketTyped),
        IsTypingSmartParenthesisHandlerAvailable);
      typingAssistManager.AddTypingHandler(lifetime, '[', this, c => WrapCppAction(c, braceHandler.HandleLeftBracketOrParenthTyped),
        IsTypingSmartParenthesisHandlerAvailable);
      typingAssistManager.AddTypingHandler(lifetime, ']', this, c => WrapCppAction(c, braceHandler.HandleRightBracketTyped),
        IsTypingSmartParenthesisHandlerAvailable);
      typingAssistManager.AddTypingHandler(lifetime, ')', this, c => WrapCppAction(c, braceHandler.HandleRightBracketTyped),
        IsTypingSmartParenthesisHandlerAvailable);
      typingAssistManager.AddTypingHandler(lifetime, '"', this, c => WrapCppAction(c, quoteHandler.HandleQuoteTyped),
        IsTypingSmartParenthesisHandlerAvailable);
      typingAssistManager.AddTypingHandler(lifetime, '\'', this, c => WrapCppAction(c, quoteHandler.HandleQuoteTyped),
        IsTypingSmartParenthesisHandlerAvailable);
      typingAssistManager.AddTypingHandler(lifetime, ';', this, c => WrapCppAction(c, braceHandler.HandleSemicolonTyped),
        IsCorrectCommonTyposAvailable);
      typingAssistManager.AddTypingHandler(lifetime, ':', this, c => WrapCppAction(c, braceHandler.HandleColonTyped),
        IsTypingHandlerAvailable);
      typingAssistManager.AddTypingHandler(lifetime, '*', this, c => WrapCppAction(c, braceHandler.HandleStarTyped),
        IsCorrectCommonTyposAvailable);
      
      // TODO: # typing should respect ShaderLabIndent
      // typingAssistManager.AddTypingHandler(lifetime, '#', this, c => WrapCppAction(c, braceHandler.HandleSharpTyped),
      //   IsCorrectCommonTyposAvailable);
      
      typingAssistManager.AddActionHandler(lifetime, TextControlActions.ActionIds.Backspace, this,
        c => WrapAction(c, deleteHandler.HandleBackspacePressed), IsActionHandlerAvailable);
      typingAssistManager.AddActionHandler(lifetime, TextControlActions.ActionIds.Enter, this,
        c => WrapAction(c, braceHandler.HandleEnterTyped), IsActionHandlerAvailable);
      typingAssistManager.AddActionHandler(lifetime, TextControlActions.ActionIds.Tab, this, HandleTabPressed,
        IsActionHandlerAvailable);
      typingAssistManager.AddActionHandler(lifetime, TextControlActions.ActionIds.TabLeft, this, HandleTabLeftPressed,
        IsActionHandlerAvailable);
      typingAssistManager.AddActionHandler(lifetime, EditorStartNewLineBeforeAction.ACTION_ID, this,
        c => WrapAction(c, braceHandler.HandleStartNewLineBeforePressed), IsActionHandlerAvailable);
    }

    private bool WrapCppAction(ITypingContext typingContext, Func<ITypingContext, bool> handleLeftBraceTyped)
    {
      if (!FindCgContent(typingContext.TextControl, out var lexer)) return false;

      if (lexer.TokenType is CppTokenNodeType)
        return handleLeftBraceTyped(typingContext);
      
      return false;
    }

    private bool FindCgContent(ITextControl textControl, out CachingLexer lexer)
    {
      var offset = textControl.Caret.DocumentOffset().Offset;
      // base is important here! 
      lexer = base.GetCachingLexer(textControl);

      if (lexer == null || !lexer.FindTokenAt(offset))
        return false;
      return true;
    }


    private bool WrapAction(IActionContext actionContext, Func<IActionContext, bool> cppAction)
    {
      if (!FindCgContent(actionContext.TextControl, out var lexer)) return false;
      
      if (lexer.TokenType is CppTokenNodeType)
        return cppAction(actionContext);
      
      // TODO, execute SL action here
      
      return false;
    }

    protected override bool IsSupported(ITextControl textControl)
    {
      var projectFile = textControl.Document.GetPsiSourceFile(Solution);
      return projectFile != null && projectFile.LanguageType.IsExactly<ShaderLabProjectFileType>() &&
             projectFile.IsValid();
    }

    public bool QuickCheckAvailability(ITextControl textControl, IPsiSourceFile projectFile)
    {
      return projectFile.LanguageType.Is<ShaderLabProjectFileType>();
    }

    public override CachingLexer GetCachingLexer(ITextControl textControl)
    {
      return myDummyFormatter.ComposeKeywordResolvingLexer(textControl);
    }
  }
}