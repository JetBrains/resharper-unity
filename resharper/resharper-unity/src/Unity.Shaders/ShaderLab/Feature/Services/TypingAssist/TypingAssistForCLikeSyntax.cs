#nullable enable
using JetBrains.Application.CommandProcessing;
using JetBrains.Application.Settings;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.StructuralRemove;
using JetBrains.ReSharper.Feature.Services.TypingAssist;
using JetBrains.ReSharper.Plugins.Unity.Common.Psi.Syntax;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CachingLexers;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Feature.Services.TypingAssist
{
    public abstract class TypingAssistForCLikeSyntax<TLanguage> : TypingAssistForCLikeLanguage<TLanguage> where TLanguage : PsiLanguageType
    {
        private readonly CLikeSyntax mySyntax;
        
        protected TypingAssistForCLikeSyntax(CLikeSyntax cLikeSyntax, ISolution solution, ISettingsStore settingsStore, CachingLexerService cachingLexerService, ICommandProcessor commandProcessor, IPsiServices psiServices, IExternalIntellisenseHost externalIntellisenseHost, SkippingTypingAssist skippingTypingAssist, LastTypingAction lastTypingAction, StructuralRemoveManager structuralRemoveManager) : 
            base(solution, settingsStore, cachingLexerService, commandProcessor, psiServices, externalIntellisenseHost, skippingTypingAssist, lastTypingAction, structuralRemoveManager)
        {
            mySyntax = cLikeSyntax;
        }

        protected sealed override TokenNodeType? LBRACE => mySyntax.LBRACE;
        protected sealed override TokenNodeType? RBRACE => mySyntax.RBRACE;
        protected sealed override TokenNodeType? LBRACKET => mySyntax.LBRACKET;
        protected sealed override TokenNodeType? RBRACKET => mySyntax.RBRACKET;
        protected sealed override TokenNodeType? LPARENTH => mySyntax.LPARENTH;
        protected sealed override TokenNodeType? RPARENTH => mySyntax.RPARENTH;
        protected sealed override TokenNodeType? WHITE_SPACE => mySyntax.WHITE_SPACE;
        protected sealed override TokenNodeType? NEW_LINE => mySyntax.NEW_LINE;
        protected sealed override TokenNodeType? END_OF_LINE_COMMENT => mySyntax.END_OF_LINE_COMMENT;
        protected sealed override TokenNodeType? C_STYLE_COMMENT => mySyntax.C_STYLE_COMMENT;
        protected sealed override TokenNodeType? PLUS => mySyntax.PLUS;
        protected sealed override TokenNodeType? SEMICOLON => mySyntax.SEMICOLON;
        protected sealed override TokenNodeType? DOT => mySyntax.DOT;
        protected sealed override NodeTypeSet STRING_LITERALS => mySyntax.STRING_LITERALS;
        protected sealed override NodeTypeSet ACCESS_CHAIN_TOKENS => mySyntax.ACCESS_CHAIN_TOKENS;
        protected sealed override bool IsLBrace(ITextControl textControl, ITreeNode node) => node.NodeType == LBRACE;
        protected sealed override bool IsRBrace(ITextControl textControl, ITreeNode node) => node.NodeType == RBRACE;
        protected sealed override bool IsSemicolon(ITextControl textControl, ITreeNode node) => node.NodeType == SEMICOLON;
        protected sealed override bool IsLBrace(ITextControl textControl, CachingLexer lexer) => lexer.TokenType == LBRACE;
        protected sealed override bool IsRBrace(ITextControl textControl, CachingLexer lexer) => lexer.TokenType == RBRACE;
    }
}