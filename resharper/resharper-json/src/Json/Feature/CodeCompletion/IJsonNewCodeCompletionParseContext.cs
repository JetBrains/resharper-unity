using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Json.Feature.CodeCompletion
{
    public interface IJsonNewCodeCompletionParseContext
    {
        [CanBeNull] ITreeNode TreeNode { get; }
        [CanBeNull] IReference Reference { get; }
        int StartOffset { get; }
        DocumentRange ToDocumentRange(TreeTextRange treeRange);
    }
}