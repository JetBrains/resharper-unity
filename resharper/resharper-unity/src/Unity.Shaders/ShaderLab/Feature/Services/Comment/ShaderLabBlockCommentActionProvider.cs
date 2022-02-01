using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Comment;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Parsing;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Feature.Services.Comment
{
    [Language(typeof(ShaderLabLanguage))]
    public class ShaderLabBlockCommentActionProvider : IBlockCommentActionProvider
    {
        public TextRange GetBlockComment(ITokenNode tokenNode)
        {
            return tokenNode.GetTokenType() == ShaderLabTokenType.MULTI_LINE_COMMENT
                ? new TextRange(tokenNode.GetDocumentStartOffset().Offset, tokenNode.GetDocumentEndOffset().Offset)
                : TextRange.InvalidRange;
        }

        public int InsertBlockCommentPosition(ITokenNode tokenNode, int position)
        {
            return position == tokenNode.GetDocumentStartOffset().Offset ? position : tokenNode.GetDocumentEndOffset().Offset;
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