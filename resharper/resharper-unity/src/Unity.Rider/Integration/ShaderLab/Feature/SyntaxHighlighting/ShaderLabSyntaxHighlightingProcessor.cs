using JetBrains.ReSharper.Daemon.SyntaxHighlighting;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Daemon.Stages;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Parsing;
using JetBrains.ReSharper.Psi.Parsing;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.ShaderLab.Feature.SyntaxHighlighting
{
    internal class ShaderLabSyntaxHighlightingProcessor : SyntaxHighlightingProcessor
    {
        public override string GetAttributeId(TokenNodeType tokenType)
        {
            if (tokenType == ShaderLabTokenType.CG_CONTENT)
                return ShaderLabHighlightingAttributeIds.INJECTED_LANGUAGE_FRAGMENT;

            return base.GetAttributeId(tokenType);
        }

        protected override bool IsLineComment(TokenNodeType tokenType)
        {
            return tokenType == ShaderLabTokenType.END_OF_LINE_COMMENT;
        }

        protected override bool IsBlockComment(TokenNodeType tokenType)
        {
            return tokenType == ShaderLabTokenType.MULTI_LINE_COMMENT;
        }

        protected override bool IsNumber(TokenNodeType tokenType)
        {
            return tokenType == ShaderLabTokenType.NUMERIC_LITERAL || tokenType == ShaderLabTokenType.PP_DIGITS;
        }

        protected override bool IsKeyword(TokenNodeType tokenType)
        {
            return base.IsKeyword(tokenType) ||
                   tokenType == ShaderLabTokenType.PP_ERROR
                   || tokenType == ShaderLabTokenType.PP_WARNING
                   || tokenType == ShaderLabTokenType.PP_LINE
                   // Technically, these are also a kind of preprocessor token
                   || tokenType == ShaderLabTokenType.CG_INCLUDE
                   || tokenType == ShaderLabTokenType.CG_PROGRAM
                   || tokenType == ShaderLabTokenType.CG_END
                   || tokenType == ShaderLabTokenType.GLSL_INCLUDE
                   || tokenType == ShaderLabTokenType.GLSL_PROGRAM
                   || tokenType == ShaderLabTokenType.GLSL_END
                   || tokenType == ShaderLabTokenType.HLSL_INCLUDE
                   || tokenType == ShaderLabTokenType.HLSL_PROGRAM
                   || tokenType == ShaderLabTokenType.HLSL_END;
        }

        protected override string NumberAttributeId => ShaderLabHighlightingAttributeIds.NUMBER;

        protected override string KeywordAttributeId => ShaderLabHighlightingAttributeIds.KEYWORD;

        protected override string StringAttributeId => ShaderLabHighlightingAttributeIds.STRING;

        protected override string LineCommentAttributeId => ShaderLabHighlightingAttributeIds.LINE_COMMENT;

        protected override string BlockCommentAttributeId => ShaderLabHighlightingAttributeIds.BLOCK_COMMENT;
    }
}