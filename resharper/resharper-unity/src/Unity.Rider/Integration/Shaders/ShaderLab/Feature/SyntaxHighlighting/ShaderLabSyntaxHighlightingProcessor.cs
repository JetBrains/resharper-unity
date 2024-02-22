using JetBrains.ReSharper.Daemon.Syntax;
using JetBrains.ReSharper.Daemon.SyntaxHighlighting;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Parsing;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Daemon.Stages;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Shaders.ShaderLab.Feature.SyntaxHighlighting
{
    internal class ShaderLabSyntaxHighlightingProcessor : SyntaxHighlightingProcessor
    {
        private string GetAttributeId(ITreeNode treeNode, TokenNodeType tokenType)
        {
            if (tokenType == ShaderLabTokenType.CG_CONTENT)
                return ShaderLabHighlightingAttributeIds.INJECTED_LANGUAGE_FRAGMENT;
            if (tokenType is IShaderLabTokenNodeType shaderLabTokenNodeType)
            {
                switch (shaderLabTokenNodeType.GetKeywordType(treeNode))
                {
                    case ShaderLabKeywordType.BlockCommand:
                        return ShaderLabHighlightingAttributeIds.BLOCK_COMMAND;
                    case ShaderLabKeywordType.RegularCommand:
                        return ShaderLabHighlightingAttributeIds.COMMAND;
                    case ShaderLabKeywordType.PropertyType:
                        return ShaderLabHighlightingAttributeIds.PROPERTY_TYPE;
                    case ShaderLabKeywordType.CommandArgument:
                        return ShaderLabHighlightingAttributeIds.COMMAND_ARGUMENT;
                }
            }

            return base.GetAttributeId(tokenType);
        }
        
        public override void ProcessBeforeInterior(ITreeNode element, IHighlightingConsumer context)
        {
            if (element is not ITokenNode tokenNode) return;
            var tokenNodeType = tokenNode.GetTokenType();
            if (tokenNodeType.IsWhitespace) return;
            var range = tokenNode.GetDocumentRange();
            if (range.TextRange.IsEmpty) return;
            var attributeId = GetAttributeId(element, tokenNodeType);
            if (!string.IsNullOrEmpty(attributeId)) 
                context.AddHighlighting(new ReSharperSyntaxHighlighting(attributeId, null, range));
        }

        protected override bool IsLineComment(TokenNodeType tokenType) => tokenType == ShaderLabTokenType.END_OF_LINE_COMMENT;

        protected override bool IsBlockComment(TokenNodeType tokenType) => tokenType == ShaderLabTokenType.MULTI_LINE_COMMENT;

        protected override bool IsNumber(TokenNodeType tokenType) => tokenType == ShaderLabTokenType.NUMERIC_LITERAL || tokenType == ShaderLabTokenType.PP_DIGITS;

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