using JetBrains.ReSharper.Plugins.Unity.Common.Psi.Syntax;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Parsing;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Syntax
{
    public static class ShaderLabSyntax
    {
        private static readonly NodeTypeSet ourStringLiterals = new(ShaderLabTokenType.STRING_LITERAL, ShaderLabTokenType.UNQUOTED_STRING_LITERAL);
        
        public static CLikeSyntax CLike = new()
        {
            LBRACE = ShaderLabTokenType.LBRACE,
            RBRACE = ShaderLabTokenType.RBRACE,
            LBRACKET = ShaderLabTokenType.LBRACK,
            RBRACKET = ShaderLabTokenType.RBRACK,
            LPARENTH = ShaderLabTokenType.LPAREN,
            RPARENTH = ShaderLabTokenType.RPAREN,
            WHITE_SPACE = ShaderLabTokenType.WHITESPACE,
            NEW_LINE = ShaderLabTokenType.NEW_LINE,
            END_OF_LINE_COMMENT = ShaderLabTokenType.END_OF_LINE_COMMENT,
            C_STYLE_COMMENT = ShaderLabTokenType.MULTI_LINE_COMMENT,
            PLUS = ShaderLabTokenType.PLUS,
            DOT = ShaderLabTokenType.DOT,
            STRING_LITERALS = ourStringLiterals  
        };
    }
}