using JetBrains.ReSharper.Daemon.SyntaxHighlighting;
using JetBrains.ReSharper.Plugins.Unity.Cg.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Parsing.TokenNodeTypes;
using JetBrains.ReSharper.Psi.Parsing;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Cg.Daemon.SyntaxHighlighting
{
    public class CgSyntaxHighlightingProcessor : SyntaxHighlightingProcessor
    {
        protected override bool IsLineComment(TokenNodeType tokenType)
        {
            return tokenType == CgTokenNodeTypes.SINGLE_LINE_COMMENT;
        }

        protected override bool IsBlockComment(TokenNodeType tokenType)
        {
            return tokenType == CgTokenNodeTypes.DELIMITED_COMMENT || tokenType == CgTokenNodeTypes.UNFINISHED_DELIMITED_COMMENT;
        }

        protected override bool IsNumber(TokenNodeType tokenType)
        {
            return tokenType == CgTokenNodeTypes.NUMERIC_LITERAL;
        }

        protected override string NumberAttributeId => CgHighlightingAttributeIds.NUMBER;
        protected override string KeywordAttributeId => CgHighlightingAttributeIds.KEYWORD;
        protected override string LineCommentAttributeId => CgHighlightingAttributeIds.LINE_COMMENT;
        protected override string BlockCommentAttributeId => CgHighlightingAttributeIds.DELIMITED_COMMENT;
    }
}