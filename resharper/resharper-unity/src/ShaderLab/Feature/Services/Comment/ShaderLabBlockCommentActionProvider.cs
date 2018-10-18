using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Comment;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Parsing;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Feature.Services.Comment
{
    [Language(typeof(ShaderLabLanguage))]
    public class ShaderLabBlockCommentActionProvider : IBlockCommentActionProvider
    {
        public TextRange GetBlockComment(CachingLexer lexer)
        {
            return lexer.TokenType == ShaderLabTokenType.MULTI_LINE_COMMENT
                ? new TextRange(lexer.TokenStart, lexer.TokenEnd)
                : TextRange.InvalidRange;
        }

        public int InsertBlockCommentPosition(ILexer lexer, int position)
        {
            return position == lexer.TokenStart ? position : lexer.TokenEnd;
        }

        public bool IsAvailable(IFile file, DocumentRange range, out bool disableAllProviders)
        {
            disableAllProviders = false;
            return true;
        }

        public string StartBlockCommentMarker => "/*";
        public string EndBlockCommentMarker => "*/";
        public string NestedStartBlockCommentMarker => null;
        public string NestedEndBlockCommentMarker => "#";
    }
}