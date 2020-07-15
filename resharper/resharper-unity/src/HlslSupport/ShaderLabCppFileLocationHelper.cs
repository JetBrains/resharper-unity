using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Parsing;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Cpp.Caches;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.HlslSupport
{
    public static class ShaderLabCppHelper
    {
        public static IEnumerable<(CppFileLocation Location, ShaderProgramType ProgramType)> GetCppFileLocations(
            IPsiSourceFile sourceFile)
        {
            var lexer = new ShaderLabLexerGenerated(sourceFile.Document.Buffer);
            lexer.Start();
            while (lexer.TokenType != null)
            {

                var type = GetProgramType(lexer.TokenType);
                if (type != ShaderProgramType.Uknown)
                {
                    lexer.Advance();
                    if (lexer.TokenType == null)
                        yield break;
                    
                    yield return (new CppFileLocation(sourceFile, new TextRange(lexer.TokenStart, lexer.TokenEnd)), type);
                }
                lexer.Advance();
            }
        }

        private static ShaderProgramType GetProgramType(TokenNodeType lexerTokenType)
        {
            if (lexerTokenType == ShaderLabTokenType.CG_INCLUDE)
                return ShaderProgramType.CGInclude;
            if (lexerTokenType == ShaderLabTokenType.CG_PROGRAM)
                return ShaderProgramType.CGProgram;
            if (lexerTokenType == ShaderLabTokenType.HLSL_INCLUDE)
                return ShaderProgramType.HLSLInclude;
            if (lexerTokenType == ShaderLabTokenType.HLSL_PROGRAM)
                return ShaderProgramType.HLSLProgram;
            if (lexerTokenType == ShaderLabTokenType.GLSL_INCLUDE)
                return ShaderProgramType.GLSLInclude;
            if (lexerTokenType == ShaderLabTokenType.GLSL_PROGRAM)
                return ShaderProgramType.GLSLProgram;

            return ShaderProgramType.Uknown;
        }
    }
}