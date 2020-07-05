using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Parsing;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Cpp.Caches;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.HlslSupport
{
    public static class ShaderLabCppHelper
    {
        public static IEnumerable<(CppFileLocation Location, bool IsInclude)> GetCppFileLocations(
            IPsiSourceFile sourceFile)
        {
            var lexer = new ShaderLabLexerGenerated(sourceFile.Document.Buffer);
            lexer.Start();
            bool prevCGInclude = false;
            while (lexer.TokenType != null)
            {
                if (lexer.TokenType == ShaderLabTokenType.CG_INCLUDE)
                    prevCGInclude = true;
                else if (lexer.TokenType == ShaderLabTokenType.CG_CONTENT)
                {
                    yield return (new CppFileLocation(sourceFile, new TextRange(lexer.TokenStart, lexer.TokenEnd)),
                        prevCGInclude);
                    prevCGInclude = false;
                }
                else if (!lexer.TokenType.IsFiltered)
                    prevCGInclude = false;

                lexer.Advance();
            }
        }
    }
}