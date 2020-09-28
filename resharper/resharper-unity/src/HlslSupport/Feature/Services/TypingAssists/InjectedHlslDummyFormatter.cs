using JetBrains.Application.Settings;
using JetBrains.Diagnostics;
using JetBrains.DocumentManagers;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Cpp.TypingAssist;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Parsing;
using JetBrains.ReSharper.Psi.CachingLexers;
using JetBrains.ReSharper.Psi.CodeStyle;
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
    public InjectedHlslDummyFormatter(ISolution solution, CachingLexerService cachingLexerService, DocumentToProjectFileMappingStorage projectFileMappingStorage, IGlobalFormatSettingsService formatSettings,
      ISettingsStore settingsStore, ISettingsOptimization settingsOptimization) : base(solution, cachingLexerService, projectFileMappingStorage, formatSettings, settingsStore, settingsOptimization)
    {
    }

    public override CppCachingKeywordResolvingLexer ComposeKeywordResolvingLexer(ITextControl textControl)
    {
      var dialect = new CppHLSLDialect(true, false);
      var cachingLexer = new ShaderLabLexerGenerated(textControl.Document.Buffer, CppLexer.Create).ToCachingLexer().TokenBuffer.CreateLexer();
      
      return new CppCachingKeywordResolvingLexer(cachingLexer, dialect);
    }

    public override string CalculateInjectionIndent(CachingLexer lexer, ITextControl textControl)
    {
      var offset = textControl.Caret.DocumentOffset();
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
          var doc = textControl.Document;
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
  }
}