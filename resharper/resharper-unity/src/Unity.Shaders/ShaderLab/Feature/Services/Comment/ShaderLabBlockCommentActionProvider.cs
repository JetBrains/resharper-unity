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
        public DocumentRange GetBlockComment(ITokenNode tokenNode)
        {
            return tokenNode.GetTokenType() == ShaderLabTokenType.MULTI_LINE_COMMENT
                ? new DocumentRange(tokenNode.GetDocumentStartOffset(), tokenNode.GetDocumentEndOffset())
                : DocumentRange.InvalidRange;
        }

        public DocumentOffset InsertBlockCommentPosition(ITokenNode tokenNode, DocumentOffset position)
        {
            return position == tokenNode.GetDocumentStartOffset() ? position : tokenNode.GetDocumentEndOffset();
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