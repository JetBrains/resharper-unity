using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Parsing;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Cpp.Caches;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.HlslSupport.Integration.Injections
{
    public static class InjectedHlslLocationHelper
    {
        public static IEnumerable<(CppFileLocation Location, InjectedHlslProgramType ProgramType)> GetCppFileLocations(
            IPsiSourceFile sourceFile)
        {
            var lexer = new ShaderLabLexerGenerated(sourceFile.Document.Buffer);
            lexer.Start();
            while (lexer.TokenType != null)
            {

                var type = GetProgramType(lexer.TokenType);
                if (type != InjectedHlslProgramType.Uknown)
                {
                    lexer.Advance();
                    if (lexer.TokenType == null)
                        yield break;
                    
                    yield return (new CppFileLocation(sourceFile, new TextRange(lexer.TokenStart, lexer.TokenEnd)), type);
                }
                lexer.Advance();
            }
        }

        private static InjectedHlslProgramType GetProgramType(TokenNodeType lexerTokenType)
        {
            if (lexerTokenType == ShaderLabTokenType.CG_INCLUDE)
                return InjectedHlslProgramType.CGInclude;
            if (lexerTokenType == ShaderLabTokenType.CG_PROGRAM)
                return InjectedHlslProgramType.CGProgram;
            if (lexerTokenType == ShaderLabTokenType.HLSL_INCLUDE)
                return InjectedHlslProgramType.HLSLInclude;
            if (lexerTokenType == ShaderLabTokenType.HLSL_PROGRAM)
                return InjectedHlslProgramType.HLSLProgram;

            return InjectedHlslProgramType.Uknown;
        }
    }
}