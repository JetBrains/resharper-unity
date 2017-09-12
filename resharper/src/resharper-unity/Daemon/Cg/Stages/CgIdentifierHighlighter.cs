using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Psi.Cg.Tree;
using JetBrains.ReSharper.Psi.Tree;
using IIdentifier = JetBrains.ReSharper.Plugins.Unity.Psi.Cg.Tree.IIdentifier;

namespace JetBrains.ReSharper.Plugins.Unity.Daemon.Cg.Stages
{
    public class CgIdentifierHighlighter
    {
        public void Highlight(ITreeNode node, IHighlightingConsumer context)
        {
            if (node is ITypeReference)
                context.AddHighlighting(new CgIdentifierHighlighting(HighlightingAttributeIds.TYPE_CLASS_ATTRIBUTE, node.GetDocumentRange()));
            else if (node is ISemantic)
                context.AddHighlighting(new CgIdentifierHighlighting(HighlightingAttributeIds.KEYWORD, node.GetDocumentRange())); // TODO: add as proper keywords maybe
        }
    }
}