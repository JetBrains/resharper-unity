using System;
using JetBrains.Annotations;
using JetBrains.Application.CommandProcessing;
using JetBrains.Application.Environment;
using JetBrains.Application.Environment.Helpers;
using JetBrains.Application.Parts;
using JetBrains.Application.Settings;
using JetBrains.Application.UI.ActionSystem.Text;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.Cpp.TypingAssist;
using JetBrains.ReSharper.Feature.Services.StructuralRemove;
using JetBrains.ReSharper.Feature.Services.TypingAssist;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CachingLexers;
using JetBrains.ReSharper.Psi.Cpp.Parsing;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.TextControl;
using JetBrains.Util;

// @formatter:indent_size 2
namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Feature.Services.TypingAssists
{
  [SolutionComponent(InstantiationEx.LegacyDefault)]
  public class InjectedHlslTypingAssist : TypingAssistLanguageBase<ShaderLabLanguage>, ITypingHandler
  { 
    [NotNull] private readonly InjectedHlslDummyFormatter myDummyFormatter;

    public InjectedHlslTypingAssist(
      Lifetime lifetime,
      [NotNull] TypingAssistDependencies dependencies,
      [NotNull] InjectedHlslDummyFormatter dummyFormatter)
      : base(dependencies)
    { 
      myDummyFormatter = dummyFormatter;
      var braceHandler = new InjectedHlslBraceHandler(this, dummyFormatter);
      var quoteHandler = new CppQuoteHandler<ShaderLabLanguage>(this);
      var deleteHandler = new CppDeleteHandler<ShaderLabLanguage>(this, dummyFormatter, braceHandler);

      var typingAssistManager = dependencies.TypingAssistManager;
      
      typingAssistManager.AddTypingHandler(lifetime, '{', this, c => ExecuteTypingInCppContextOnly(c, braceHandler.HandleLeftBraceTyped),
        IsTypingHandlerAvailable);
      typingAssistManager.AddTypingHandler(lifetime, '}', this, c => ExecuteTypingInCppContextOnly(c, braceHandler.HandleRightBraceTyped),
        IsTypingHandlerAvailable);
      typingAssistManager.AddTypingHandler(lifetime, '(', this, c => ExecuteTypingInCppContextOnly(c, braceHandler.HandleLeftBracketOrParenthTyped),
        IsTypingSmartParenthesisHandlerAvailable);
      typingAssistManager.AddTypingHandler(lifetime, '<', this, c => ExecuteTypingInCppContextOnly(c, braceHandler.HandleLTTyped),
        IsTypingSmartParenthesisHandlerAvailable);
      typingAssistManager.AddTypingHandler(lifetime, '>', this, c => ExecuteTypingInCppContextOnly(c, braceHandler.HandleRightBracketTyped),
        IsTypingSmartParenthesisHandlerAvailable);
      typingAssistManager.AddTypingHandler(lifetime, '[', this, c => ExecuteTypingInCppContextOnly(c, braceHandler.HandleLeftBracketOrParenthTyped),
        IsTypingSmartParenthesisHandlerAvailable);
      typingAssistManager.AddTypingHandler(lifetime, ']', this, c => ExecuteTypingInCppContextOnly(c, braceHandler.HandleRightBracketTyped),
        IsTypingSmartParenthesisHandlerAvailable);
      typingAssistManager.AddTypingHandler(lifetime, ')', this, c => ExecuteTypingInCppContextOnly(c, braceHandler.HandleRightBracketTyped),
        IsTypingSmartParenthesisHandlerAvailable);
      typingAssistManager.AddTypingHandler(lifetime, '"', this, c => ExecuteTypingInCppContextOnly(c, quoteHandler.HandleQuoteTyped),
        IsTypingSmartParenthesisHandlerAvailable);
      typingAssistManager.AddTypingHandler(lifetime, '\'', this, c => ExecuteTypingInCppContextOnly(c, quoteHandler.HandleQuoteTyped),
        IsTypingSmartParenthesisHandlerAvailable);
      typingAssistManager.AddTypingHandler(lifetime, ';', this, c => ExecuteTypingInCppContextOnly(c, braceHandler.HandleSemicolonTyped),
        IsCorrectCommonTyposAvailable);
      typingAssistManager.AddTypingHandler(lifetime, ':', this, c => ExecuteTypingInCppContextOnly(c, braceHandler.HandleColonTyped),
        IsTypingHandlerAvailable);
      typingAssistManager.AddTypingHandler(lifetime, '*', this, c => ExecuteTypingInCppContextOnly(c, braceHandler.HandleStarTyped),
        IsCorrectCommonTyposAvailable);
      
      typingAssistManager.AddActionHandler(lifetime, TextControlActions.ActionIds.Backspace, this,
        c => ExecuteActionInCppContextOnly(c, deleteHandler.HandleBackspacePressed), IsActionHandlerAvailable);
      typingAssistManager.AddActionHandler(lifetime, TextControlActions.ActionIds.Enter, this,
        c => ExecuteActionInCppContextOnly(c, braceHandler.HandleEnterTyped), IsActionHandlerAvailable);
      typingAssistManager.AddActionHandler(lifetime, EditorStartNewLineBeforeAction.ACTION_ID, this,
        c => ExecuteActionInCppContextOnly(c, braceHandler.HandleStartNewLineBeforePressed), IsActionHandlerAvailable);
    }

    private bool ExecuteTypingInCppContextOnly(ITypingContext typingContext, Func<ITypingContext, bool> handleLeftBraceTyped)
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

    private bool ExecuteActionInCppContextOnly(IActionContext actionContext, Func<IActionContext, bool> cppAction)
    { 
      if (actionContext.EnsureWritable() != EnsureWritableResult.SUCCESS)
        return false;
      
      if (!FindCgContent(actionContext.TextControl, out var lexer)) return false;

      if (lexer.TokenType is CppTokenNodeType)
        return cppAction(actionContext);

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