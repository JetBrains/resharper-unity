using JetBrains.Diagnostics;
using JetBrains.DocumentManagers;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Cpp.TypingAssist;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Integration.Cpp;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Parsing;
using JetBrains.ReSharper.Psi.CachingLexers;
using JetBrains.ReSharper.Psi.Cpp.Language;
using JetBrains.ReSharper.Psi.Cpp.Parsing;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Feature.Services.TypingAssists
{
    [SolutionComponent]
    public class InjectedHlslDummyFormatter : CppDummyFormatterBase
    {
        private readonly ISolution mySolution;
        private readonly UnityHlslCppCompilationPropertiesProvider myCompilationPropertiesProvider;

        public InjectedHlslDummyFormatter(ISolution solution, CachingLexerService cachingLexerService,
            DocumentToProjectFileMappingStorage projectFileMappingStorage, UnityHlslCppCompilationPropertiesProvider compilationPropertiesProvider)
            : base(solution, cachingLexerService, projectFileMappingStorage)
        {
            mySolution = solution;
            myCompilationPropertiesProvider = compilationPropertiesProvider;
        }

        public CppCachingKeywordResolvingLexer ComposeKeywordResolvingLexer(ITextControl textControl)
        {
            var dialect = myCompilationPropertiesProvider.ShaderLabHlslDialect;
            var cachingLexer = new ShaderLabLexerGenerated(textControl.Document.Buffer, CppLexer.Create).ToCachingLexer().TokenBuffer.CreateLexer();
            return new CppCachingKeywordResolvingLexer(cachingLexer, dialect);
        }

        public override string CalculateInjectionIndent(CppDummyFormatterContext context, CachingLexer lexer)
        {
            var offset = context.TextControl.Caret.DocumentOffset().Offset;
            using (LexerStateCookie.Create(lexer))
            {
                if (!lexer.FindTokenAt(offset))
                    return "";

                var tt = lexer.TokenType;
                while (tt != null)
                {
                    lexer.Advance(-1);
                    tt = lexer.TokenType;

                    if (tt == ShaderLabTokenType.CG_PROGRAM || tt == ShaderLabTokenType.CG_INCLUDE ||
                        tt == ShaderLabTokenType.HLSL_PROGRAM || tt == ShaderLabTokenType.HLSL_INCLUDE ||
                        tt == ShaderLabTokenType.GLSL_PROGRAM || tt == ShaderLabTokenType.GLSL_INCLUDE)
                        break;
                }

                if (tt != null)
                {
                    var blockStartPos = lexer.CurrentPosition;
                    for (lexer.Advance(); lexer.TokenEnd <= offset && (tt = lexer.TokenType) is { IsWhitespace: true }; lexer.Advance()) { } // skip to next non-whitespace token 

                    // if next non-empty token is end of block then reset to block start otherwise use offset of first line in the block 
                    if (tt == ShaderLabTokenType.CG_END ||
                        tt == ShaderLabTokenType.HLSL_END ||
                        tt == ShaderLabTokenType.GLSL_END)
                        lexer.CurrentPosition = blockStartPos;
                    
                    return CalculateLineIndent(context, lexer);
                }
            }

            return "";
        }

        private static string CalculateLineIndent(CppDummyFormatterContext context, CachingLexer lexer)
        {
            var doc = context.TextControl.Document;
            var lineStart = doc.GetLineStartOffset(doc.GetCoordsByOffset(lexer.TokenStart).Line);
            lexer.FindTokenAt(lineStart);
            var tt = lexer.TokenType;

            Assertion.AssertNotNull(tt, "Lexer.TokenType may not be null");
            while (tt!.IsWhitespace)
            {
                lexer.Advance();
                tt = lexer.TokenType;
            }

            var tokenLineStart = doc.GetLineStartOffset(doc.GetCoordsByOffset(lexer.TokenStart).Line);
            return doc.GetText(new TextRange(tokenLineStart, lexer.TokenStart));
        }

        private class HlslDummyFormatterContext : CppDummyFormatterContext
        {
            public HlslDummyFormatterContext(ISolution solution, ITextControl originalTextControl,
                ITextControl textControl, CppLanguageDialect dialect)
                : base(solution, originalTextControl, textControl, dialect)
            {
            }

            public override CppCachingKeywordResolvingLexer ComposeKeywordResolvingLexer()
            {
                var cachingLexer = new ShaderLabLexerGenerated(Document.Buffer, CppLexer.Create).ToCachingLexer().TokenBuffer.CreateLexer();
                return new CppCachingKeywordResolvingLexer(cachingLexer, Dialect);
            }
        }

        public override CppDummyFormatterContext CreateContext(ITextControl textControl,
            ITextControl originalTextControl)
        {
            var dialect = myCompilationPropertiesProvider.ShaderLabHlslDialect;
            return new HlslDummyFormatterContext(mySolution, originalTextControl, textControl, dialect);
        }
    }
}