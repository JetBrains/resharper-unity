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
using JetBrains.ReSharper.Feature.Services.Options;
using JetBrains.ReSharper.Feature.Services.StructuralRemove;
using JetBrains.ReSharper.Feature.Services.TypingAssist;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Formatting;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Parsing;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CachingLexers;
using JetBrains.ReSharper.Psi.CodeStyle;
using JetBrains.ReSharper.Psi.Cpp.Parsing;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.HlslSupport.Feature.Services.TypingAssists
{
  [SolutionComponent]
  public class ShaderLabTypingAssist : TypingAssistLanguageBase<ShaderLabLanguage>, ITypingHandler
  {
      [NotNull] private readonly CachingLexerService myCachingLexerService;
      [NotNull] private readonly HlslInShaderLabDummyFormatter myShaderLabDummyFormatter;

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
      [NotNull] LastTypingAction lastTypingAssistAction,
      [NotNull] HlslInShaderLabDummyFormatter shaderLabDummyFormatter,
      [NotNull] StructuralRemoveManager structuralRemoveManager)
      : base(solution, settingsStore, cachingLexerService, commandProcessor, psiServices, externalIntellisenseHost,
        skippingTypingAssist, lastTypingAssistAction, structuralRemoveManager)
    {
        myCachingLexerService = cachingLexerService;
        myShaderLabDummyFormatter = shaderLabDummyFormatter;
      var braceHandler = new InjectedHlslBraceHandler(this, shaderLabDummyFormatter, false, productConfigurations.IsInternalMode());
      var quoteHandler = new CppQuoteHandler<ShaderLabLanguage>(this);
      var deleteHandler = new CppDeleteHandler<ShaderLabLanguage>(this, shaderLabDummyFormatter);

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
        c => WrapAction(c, braceHandler.HandleEnterTyped, WrapEnterAction), IsActionHandlerAvailable);
      typingAssistManager.AddActionHandler(lifetime, TextControlActions.ActionIds.Tab, this, HandleTabPressed,
        IsActionHandlerAvailable);
      typingAssistManager.AddActionHandler(lifetime, TextControlActions.ActionIds.TabLeft, this, HandleTabLeftPressed,
        IsActionHandlerAvailable);
      typingAssistManager.AddActionHandler(lifetime, EditorStartNewLineBeforeAction.ACTION_ID, this,
        c => WrapAction(c, braceHandler.HandleStartNewLineBeforePressed), IsActionHandlerAvailable);
    }

    private bool WrapEnterAction(IActionContext arg, CachingLexer cachingLexer)
    {
        var textControl = arg.TextControl;
        if (GetTypingAssistOption(textControl, TypingAssistOptions.SmartIndentOnEnterExpression))
        {
            using (CommandProcessor.UsingCommand("Smart Enter"))
            {
                if (textControl.Selection.OneDocRangeWithCaret().Length > 0)
                    return false;

                var caret = textControl.Caret.Offset();
                if (caret == 0)
                    return false;
                
                var closedCount = 0;
                cachingLexer.FindTokenAt(caret - 1);
                var tt = cachingLexer.TokenType;
                while (tt != null)
                {
                    if (tt == ShaderLabTokenType.RBRACE)
                        closedCount++;
                    if (tt == ShaderLabTokenType.LBRACE)
                    {
                        if (closedCount == 0)
                        {
                            var line = textControl.Document.GetCoordsByOffset(cachingLexer.TokenStart).Line;
                            var lineOffset = textControl.Document.GetLineStartOffset(line);

                            if (cachingLexer.FindTokenAt(lineOffset))
                            {
                                // e.g  
                                //Shader "Custom/Test2_hlsl" {
                                //<caret>    Properties {
                                
                                //<caret>{
                                
                                //<caret>    {
                                var baseIndent = myShaderLabDummyFormatter.GetFormatSettingsService(textControl).GetIndentStr();
                                string indent;
                                if (cachingLexer.TokenType == ShaderLabTokenType.WHITESPACE)
                                    indent = cachingLexer.GetTokenText() + baseIndent;
                                else
                                    indent = baseIndent;
                                
                                textControl.Document.InsertText(caret, "\n" + indent);
                                return true; 
                            }
                        }
                        closedCount--;
                    }
                    
                    cachingLexer.Advance(-1);
                    tt = cachingLexer.TokenType;
                }
            }
        }

        return false;
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


    private bool WrapAction(IActionContext actionContext, Func<IActionContext, bool> cppAction, Func<IActionContext, CachingLexer, bool> shaderLabAction = null)
    { 
      if (actionContext.EnsureWritable() != EnsureWritableResult.SUCCESS)
        return false;
      
      if (!FindCgContent(actionContext.TextControl, out var lexer)) return false;

      if (lexer.TokenType is CppTokenNodeType)
        return cppAction(actionContext);

      if (shaderLabAction != null)
          return shaderLabAction(actionContext, lexer);
      
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
      return myShaderLabDummyFormatter.ComposeKeywordResolvingLexer(textControl);
    }
  }
}