using JetBrains.Diagnostics;
using JetBrains.DocumentManagers;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Cpp.TypingAssist;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Parsing;
using JetBrains.ReSharper.Psi.CachingLexers;
using JetBrains.ReSharper.Psi.Cpp.Language;
using JetBrains.ReSharper.Psi.Cpp.Parsing;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.HlslSupport.Feature.Services.TypingAssists
{
    [SolutionComponent]
    public class InjectedHlslDummyFormatter : CppDummyFormatterBase
    {
        private readonly ISolution mySolution;

        public InjectedHlslDummyFormatter(ISolution solution, CachingLexerService cachingLexerService,
            DocumentToProjectFileMappingStorage projectFileMappingStorage)
            : base(solution, cachingLexerService, projectFileMappingStorage)
        {
            mySolution = solution;
        }

        public CppCachingKeywordResolvingLexer ComposeKeywordResolvingLexer(ITextControl textControl)
        {
            var dialect = new CppHLSLDialect(true, false);
            var cachingLexer = new ShaderLabLexerGenerated(textControl.Document.Buffer, CppLexer.Create)
                .ToCachingLexer().TokenBuffer.CreateLexer();

            return new CppCachingKeywordResolvingLexer(cachingLexer, dialect);
        }

        public override string CalculateInjectionIndent(CppDummyFormatterContext context, CachingLexer lexer)
        {
            var offset = context.TextControl.Caret.DocumentOffset();
            using (LexerStateCookie.Create(lexer))
            {
                if (!lexer.FindTokenAt(offset.Offset))
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
                    var doc = context.TextControl.Document;
                    var lineStart = doc.GetLineStartOffset(doc.GetCoordsByOffset(lexer.TokenStart).Line);
                    lexer.FindTokenAt(lineStart);
                    tt = lexer.TokenType;

                    Assertion.AssertNotNull(tt, "Lexer.TokenType may not be null");
                    while (tt.IsWhitespace)
                    {
                        lexer.Advance();
                        tt = lexer.TokenType;
                    }

                    var tokenLineStart = doc.GetLineStartOffset(doc.GetCoordsByOffset(lexer.TokenStart).Line);
                    return doc.GetText(new TextRange(tokenLineStart, lexer.TokenStart));
                }
            }

            return "";
        }

        public class HlslDummyFormatterContext : CppDummyFormatterContext
        {
            public HlslDummyFormatterContext(ISolution solution, ITextControl originalTextControl,
                ITextControl textControl, CppLanguageDialect dialect)
                : base(solution, originalTextControl, textControl, dialect)
            {
            }

            public override CppCachingKeywordResolvingLexer ComposeKeywordResolvingLexer()
            {
                var dialect = new CppHLSLDialect(true, false);
                var cachingLexer = new ShaderLabLexerGenerated(Document.Buffer, CppLexer.Create).ToCachingLexer()
                    .TokenBuffer.CreateLexer();
                return new CppCachingKeywordResolvingLexer(cachingLexer, dialect);
            }
        }

        public override CppDummyFormatterContext CreateContext(ITextControl textControl,
            ITextControl originalTextControl)
        {
            var dialect = new CppHLSLDialect(true, false);
            return new HlslDummyFormatterContext(mySolution, originalTextControl, textControl, dialect);
        }
    }
}