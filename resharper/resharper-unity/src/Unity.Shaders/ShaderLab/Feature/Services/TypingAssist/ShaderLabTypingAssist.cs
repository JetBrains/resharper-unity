#nullable enable
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using JetBrains.Application.CommandProcessing;
using JetBrains.Application.Settings;
using JetBrains.Application.UI.ActionSystem.Text;
using JetBrains.DocumentModel;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.Options;
using JetBrains.ReSharper.Feature.Services.StructuralRemove;
using JetBrains.ReSharper.Feature.Services.TypingAssist;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Feature.Services.TypingAssists;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Formatting;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Parsing;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CachingLexers;
using JetBrains.ReSharper.Psi.CodeStyle;
using JetBrains.ReSharper.Psi.Cpp.Parsing;
using JetBrains.ReSharper.Psi.Format;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Feature.Services.TypingAssist
{
    [SolutionComponent]
    public class ShaderLabTypingAssist : TypingAssistLanguageBase<ShaderLabLanguage>, ITypingHandler
    {
        private readonly ISolution mySolution;
        private readonly InjectedHlslDummyFormatter myInjectedHlslDummyFormatter;

        public ShaderLabTypingAssist(
            Lifetime lifetime,
            ISolution solution,
            IPsiServices psiServices,
            ICommandProcessor commandProcessor,
            ISettingsStore settingsStore,
            InjectedHlslDummyFormatter injectedHlslDummyFormatter,
            CachingLexerService cachingLexerService,
            ITypingAssistManager typingAssistManager,
            IExternalIntellisenseHost externalIntellisenseHost,
            SkippingTypingAssist skippingTypingAssist,
            LastTypingAction lastTypingAssistAction,
            StructuralRemoveManager structuralRemoveManager)
            : base(solution, settingsStore, cachingLexerService, commandProcessor, psiServices,
                externalIntellisenseHost,
                skippingTypingAssist, lastTypingAssistAction, structuralRemoveManager)
        {
            mySolution = solution;
            myInjectedHlslDummyFormatter = injectedHlslDummyFormatter;

            typingAssistManager.AddActionHandler(lifetime, TextControlActions.ActionIds.Enter, this, HandleEnterAction, IsActionHandlerAvailable);
            typingAssistManager.AddActionHandler(lifetime, TextControlActions.ActionIds.Backspace, this, HandleBackspaceAction, IsActionHandlerAvailable);
            typingAssistManager.AddActionHandler(lifetime, TextControlActions.ActionIds.Tab, this, HandleTabPressed,
                IsActionHandlerAvailable);
            typingAssistManager.AddActionHandler(lifetime, TextControlActions.ActionIds.TabLeft, this,
                HandleTabLeftPressed,
                IsActionHandlerAvailable);
        }

        private bool HandleBackspaceAction(IActionContext actionContext)
        {
            if (actionContext.EnsureWritable() != EnsureWritableResult.SUCCESS)
                return false;
            
            if (ShouldIgnoreCaretPosition(actionContext.TextControl, out var _)) return false;
            
            // HLSL could not be affected by smart backspace action, performance optimization
            using (new ShaderLabDoNotFormatInjectionsCookie())
            {
                if (HandleUnindentOnBackspace(actionContext)) return true;
            }
            return false;
        }
        
        private bool HandleEnterAction(IActionContext actionContext)
        {
            if (actionContext.EnsureWritable() != EnsureWritableResult.SUCCESS)
                return false;
            if (ShouldIgnoreCaretPosition(actionContext.TextControl, out var cachingLexer)) 
                return false;
            
            var textControl = actionContext.TextControl;
            if (GetTypingAssistOption(textControl, TypingAssistOptions.SmartIndentOnEnterExpression))
            {
                using (CommandProcessor.UsingCommand("Smart Enter"))
                {
                    if (textControl.Selection.OneDocRangeWithCaret().Length > 0)
                        return false;

                    var offset = textControl.Caret.Offset();
                    if (offset == 0)
                        return false;

                    var range = TextRange.FromLength(offset, 0);
                    while (cachingLexer.TokenType is { IsWhitespace: true } && cachingLexer.TokenType != ShaderLabTokenType.NEW_LINE)
                        cachingLexer.Advance();
                    
                    var isEndOfBlock = cachingLexer.TokenType == ShaderLabTokenType.RBRACE;
                    if (isEndOfBlock)
                        range = range.SetEndTo(cachingLexer.TokenStart);
                    
                    if (!cachingLexer.FindTokenAt(offset - 1))
                        return false;
                    
                    // move to either to start of block or closest command keyword, if next symbol is end of block then only start of block is a valid reference
                    if (!MoveLexerToIdentReference(cachingLexer, !isEndOfBlock)) 
                        return false;

                    // should only append block indent if '{' is a first indentation reference, if there preceding shader command then just use same indentation.
                    // If next token is end of block then it also shouldn't be indented inside of block 
                    var stoppedAtStartOfBlock = cachingLexer.TokenType == ShaderLabTokenType.LBRACE;
                    var shouldAppendBlockIndent = stoppedAtStartOfBlock;
                    var document = textControl.Document;
                    // e.g  
                    //Shader "Custom/Test2_hlsl" {
                    //<caret>    Properties {

                    //<caret>{

                    //<caret>    {
                    var sb = new StringBuilder("\n");
                    if (!TryGetLineIndent(cachingLexer, document, out var indent))
                        return false;
                    sb.Append(indent);
                    if (shouldAppendBlockIndent)
                        sb.Append(GetFormatSettingsService(textControl).GetIndentStr());
                    var caretOffset = sb.Length;
                    // if Enter pressed before end of block then we want to insert extra empty line inside of the block
                    if (isEndOfBlock && (stoppedAtStartOfBlock || MoveLexerToIdentReference(cachingLexer, false) && TryGetLineIndent(cachingLexer, document, out indent)))
                    {
                        sb.Append("\n");
                        sb.Append(indent);
                    }
                    document.ReplaceText(range, sb.ToString());
                    textControl.Caret.MoveTo(range.StartDocOffset() + caretOffset, CaretVisualPlacement.DontScrollIfVisible);
                    return true;
                }
            }

            return false;
        }

        private bool TryGetLineIndent(CachingLexer cachingLexer, IDocument document, [MaybeNullWhen(false)] out string indent)
        {
            var savedPosition = cachingLexer.CurrentPosition;
            var line = document.GetCoordsByOffset(cachingLexer.TokenStart).Line;
            var lineOffset = document.GetLineStartOffset(line);

            var hasLineStartToken = cachingLexer.FindTokenAt(lineOffset);
            if (hasLineStartToken)
                indent = cachingLexer.TokenType == ShaderLabTokenType.WHITESPACE ? cachingLexer.GetTokenText() : string.Empty;
            else
                indent = null;

            cachingLexer.CurrentPosition = savedPosition;
            return hasLineStartToken;
        }
        
        private bool MoveLexerToIdentReference(CachingLexer cachingLexer, bool stopOnCommandKeyword)
        {
            var closedCount = 0;
            var tt = (IShaderLabTokenNodeType?)cachingLexer.TokenType;
            while (tt != null)
            {
                if (stopOnCommandKeyword && tt.IsCommandKeyword(cachingLexer))
                    return true;
                if (tt == ShaderLabTokenType.RBRACE)
                    closedCount++;
                if (tt == ShaderLabTokenType.LBRACE)
                {
                    if (closedCount == 0)
                        return true;

                    closedCount--;
                }

                cachingLexer.Advance(-1);
                tt = (IShaderLabTokenNodeType?)cachingLexer.TokenType;
            }

            return false;
        }

        private bool ShouldIgnoreCaretPosition(ITextControl textControl, [MaybeNullWhen(true)] out CachingLexer lexer)
        {
            var offset = textControl.Caret.DocumentOffset().Offset;
            // base is important here! 
            lexer = base.GetCachingLexer(textControl);

            if (lexer == null || !lexer.FindTokenAt(offset))
                return true;

            // inside HLSL program
            return lexer.TokenType is CppTokenNodeType;
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
            return myInjectedHlslDummyFormatter.ComposeKeywordResolvingLexer(textControl);
        }

        protected override IndentTypingHelper<ShaderLabLanguage> GetIndentTypingHelper() => new ShaderLabIndentTypingHelper(this);
        
        /// Expensive, avoid calling too many times.
        private FormatSettingsKeyBase GetFormatSettingsService(ITextControl textControl)
        {
            var sourceFile = textControl.TryGetSourceFiles(mySolution).FirstOrDefault();
            if (sourceFile == null) return FormatSettingsKeyBase.Default;
            return sourceFile.GetFormatterSettings(ShaderLabLanguage.Instance);
        }
    }
}