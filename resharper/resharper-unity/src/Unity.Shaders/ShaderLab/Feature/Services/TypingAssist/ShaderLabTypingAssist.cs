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
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Format;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Text;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Feature.Services.TypingAssist
{
    [SolutionComponent]
    public class ShaderLabTypingAssist : TypingAssistForCLikeSyntax<ShaderLabLanguage>, ITypingHandler
    {
        private static readonly Pair<TokenNodeType, TokenNodeType>[] ourBracketPairs = {
            Pair.Of(ShaderLabTokenType.LBRACK, ShaderLabTokenType.RBRACK),
            Pair.Of(ShaderLabTokenType.LPAREN, ShaderLabTokenType.RPAREN),
            Pair.Of(ShaderLabTokenType.LBRACE, ShaderLabTokenType.RBRACE)
        };
        private static readonly Pair<TokenNodeType, TokenNodeType>[] ourBracePairs = {
            Pair.Of(ShaderLabTokenType.LBRACE, ShaderLabTokenType.RBRACE)
        };
        
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
            : base(ShaderLabSyntax.CLike, solution, settingsStore, cachingLexerService, commandProcessor, psiServices,
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
            typingAssistManager.AddTypingHandler(lifetime, '{', this, HandleLeftBraceTyped, IsTypingHandlerAvailable);
            typingAssistManager.AddTypingHandler(lifetime, '}', this, HandleRightBraceTyped, IsTypingHandlerAvailable);
            typingAssistManager.AddTypingHandler(lifetime, '[', this, HandleLeftBracketOrParenthTyped, IsTypingHandlerAvailable);
            typingAssistManager.AddTypingHandler(lifetime, ']', this, HandleRightBracketTyped, IsTypingHandlerAvailable);
            typingAssistManager.AddTypingHandler(lifetime, '(', this, HandleLeftBracketOrParenthTyped, IsTypingHandlerAvailable);
            typingAssistManager.AddTypingHandler(lifetime, ')', this, HandleRightBracketTyped, IsTypingHandlerAvailable);
            typingAssistManager.AddTypingHandler(lifetime, '"', this, HandleQuoteTyped, IsTypingHandlerAvailable);
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
                    if (!TryGetLineIndent(cachingLexer, document, out var lineIndent))
                        return false;
                    var indent = lineIndent;
                    if (shouldAppendBlockIndent)
                        indent += GetFormatSettingsService(textControl).GetIndentStr();
                    // if Enter pressed inside of empty braces on same line and these are not on own line yet then insert line before opening brace
                    string? prologIndent = null;
                    // if Enter pressed before end of block then we want to insert extra empty line inside of the block
                    string? epilogueIndent = null;
                    if (isEndOfBlock && (stoppedAtStartOfBlock || MoveLexerToIdentReference(cachingLexer, false) && TryGetLineIndent(cachingLexer, document, out lineIndent)))
                    {
                        epilogueIndent = lineIndent;
                        sb.Append("\n");
                        sb.Append(lineIndent);
                        if (GetClosestTokenNodeTypeSkippingWhitespaces(cachingLexer, 1) == ShaderLabTokenType.RBRACE
                            && MoveToClosestTokenNodeTypeSkippingWhitespaces(cachingLexer, -1) is { IsWhitespace: false })
                        {
                            range = range.SetStartTo(cachingLexer.TokenEnd);
                            prologIndent = epilogueIndent;
                        }
                    }

                    CommitPsiOnlyAndProceedWithDirtyCaches(textControl, _ =>
                    {
                        var newLine = GetNewLineText(textControl.Document.GetPsiSourceFile(Solution));
                        var sb = new StringBuilder();
                        if (prologIndent != null)
                            sb.Append(newLine).Append(prologIndent).Append(ShaderLabTokenType.LBRACE.TokenRepresentation);
                        sb.Append(newLine).Append(indent);
                        var caretOffset = sb.Length;
                        if (epilogueIndent != null)
                            sb.Append(newLine).Append(epilogueIndent);
                        document.DeleteText(range);
                        document.InsertText(range.StartOffset, sb.ToString());
                        textControl.Caret.MoveTo(range.StartDocOffset() + caretOffset, CaretVisualPlacement.DontScrollIfVisible);
                    });
                    return true;
                }
            }

            return false;
        }
        
        private TokenNodeType? MoveToClosestTokenNodeTypeSkippingWhitespaces(CachingLexer lexer, int direction)
        {
            TokenNodeType? tt;
            do
            {
                lexer.Advance(direction);
                tt = lexer.TokenType;
            } while (tt != null && tt == WHITE_SPACE);
            return tt;
        }
        
        private TokenNodeType? GetClosestTokenNodeTypeSkippingWhitespaces(CachingLexer lexer, int direction)
        {
            using (LexerStateCookie.Create(lexer))
                return MoveToClosestTokenNodeTypeSkippingWhitespaces(lexer, direction);
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
            var tt = cachingLexer.TokenType as IShaderLabTokenNodeType;
            while (tt != null)
            {
                if (stopOnCommandKeyword && closedCount == 0 && tt.GetKeywordType(cachingLexer).IsCommandKeyword())
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
        
        protected override bool IsTokenSuitableForCloseBracket(TokenNodeType nextTokenType)
        {
            return nextTokenType == WHITE_SPACE
                   || nextTokenType == NEW_LINE
                   || nextTokenType == C_STYLE_COMMENT
                   || nextTokenType == END_OF_LINE_COMMENT
                   || nextTokenType == SEMICOLON
                   || nextTokenType == ShaderLabTokenType.COMMA
                   || nextTokenType == RBRACKET
                   || nextTokenType == RBRACE
                   || nextTokenType == RPARENTH;
        }

        protected override BracketMatcher CreateBracketMatcher() => new GenericBracketMatcher(ourBracketPairs);

        protected override bool GetPreferWrapBeforeOpSignSetting(IContextBoundSettingsStore settingsStore) => false;

        protected override bool DoReformatForSmartEnter(ITextControl textControl, TreeOffset lBraceTreePos, TreeOffset rBraceTreePos, int charPos, ITokenNode lBraceNode, ITokenNode rBraceNode, bool afterLBrace, IFile file, bool oneLine) => false;

        protected override bool IsNodeSuitableAsSemicolonFormatParent(ITreeNode node) => false;

        protected override ITreeNode? GetParentForFormatOnSemicolon(ITreeNode node) => null;

        protected override bool CheckThatCLikeLineEndsInOpenStringLiteral(ITextControl textControl, CachingLexer lexer, int lineEndPos, char c, NodeTypeSet correspondingTokenType, bool isStringWithAt, ref int charPos, bool defaultReturnValue)
        {
            return lexer.FindTokenAt(lineEndPos)
                   && lexer.TokenType == ShaderLabTokenType.STRING_LITERAL
                   && (lexer.GetTokenLength() == 1 || lexer.Buffer[lineEndPos] != '"'); // either '"' or unfinished string
        }

        protected override bool IsNextCharDoesNotStartNewLiteral(ITypingContext typingContext, CachingLexer lexer, int charPos, IBuffer buffer) => lexer.TokenStart != charPos && buffer[lexer.TokenStart] == typingContext.Char;

        protected override bool IsStopperTokenForStringLiteral(TokenNodeType tokenType)
        {
            return tokenType == WHITE_SPACE
                   || tokenType == NEW_LINE
                   || tokenType == C_STYLE_COMMENT
                   || tokenType == END_OF_LINE_COMMENT
                   || tokenType == SEMICOLON
                   || tokenType == PLUS
                   || tokenType == ShaderLabTokenType.COMMA
                   || tokenType == RBRACKET
                   || tokenType == RBRACE
                   || tokenType == RPARENTH
                   || STRING_LITERALS.Contains(tokenType);
        }

        protected override BracketMatcher CreateBraceMatcher() => new GenericBracketMatcher(ourBracePairs);

        protected override bool GetAutoInsertDataForRBrace(ITextControl textControl, ITokenNode rBraceToken, TreeTextRange treeLBraceRange, int lBracePos, int position, IDocument document, out TreeOffset positionForRBrace, out string rBraceText, ref IFile file)
        {
            positionForRBrace = rBraceToken.GetTreeEndOffset();
            rBraceText = "}";
            return false;
        }

        public override Pair<ITreeRange, ITreeRangePointer> GetRangeToFormatAfterRBrace(ITextControl textControl) => default;
    }
}