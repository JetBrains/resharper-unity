using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.CommandProcessing;
using JetBrains.Application.Environment;
using JetBrains.Application.Settings;
using JetBrains.Application.UI.ActionSystem.Text;
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
        [NotNull] private readonly ISolution mySolution;
        [NotNull] private readonly InjectedHlslDummyFormatter myInjectedHlslDummyFormatter;
        [NotNull] private readonly CachingLexerService myCachingLexerService;

        public ShaderLabTypingAssist(
            Lifetime lifetime,
            [NotNull] ISolution solution,
            [NotNull] IPsiServices psiServices,
            [NotNull] ICommandProcessor commandProcessor,
            [NotNull] ISettingsStore settingsStore,
            [NotNull] InjectedHlslDummyFormatter injectedHlslDummyFormatter,
            [NotNull] RunsProducts.ProductConfigurations productConfigurations,
            [NotNull] CachingLexerService cachingLexerService,
            [NotNull] ITypingAssistManager typingAssistManager,
            [NotNull] IExternalIntellisenseHost externalIntellisenseHost,
            [NotNull] SkippingTypingAssist skippingTypingAssist,
            [NotNull] LastTypingAction lastTypingAssistAction,
            [NotNull] StructuralRemoveManager structuralRemoveManager)
            : base(solution, settingsStore, cachingLexerService, commandProcessor, psiServices,
                externalIntellisenseHost,
                skippingTypingAssist, lastTypingAssistAction, structuralRemoveManager)
        {
            mySolution = solution;
            myInjectedHlslDummyFormatter = injectedHlslDummyFormatter;
            myCachingLexerService = cachingLexerService;

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
            if (ShouldIgnoreCaretPosition(actionContext.TextControl, out var cachingLexer)) return false;
            
            var textControl = actionContext.TextControl;
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
                                    var baseIndent = GetFormatSettingsService(textControl).GetIndentStr();
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

        private bool ShouldIgnoreCaretPosition(ITextControl textControl, out CachingLexer lexer)
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
        public FormatSettingsKeyBase GetFormatSettingsService(ITextControl textControl)
        {
            var sourceFile = textControl.TryGetSourceFiles(mySolution).FirstOrDefault();
            if (sourceFile == null) return FormatSettingsKeyBase.Default;
            return sourceFile.GetFormatterSettings(ShaderLabLanguage.Instance);
        }
    }
}