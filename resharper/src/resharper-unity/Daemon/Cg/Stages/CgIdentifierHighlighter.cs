using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.Tree;
using IIdentifier = JetBrains.ReSharper.Plugins.Unity.Psi.Cg.Tree.IIdentifier;

namespace JetBrains.ReSharper.Plugins.Unity.Daemon.Cg.Stages
{
    public class CgIdentifierHighlighter
    {
        public void Highlight(ITreeNode node, IHighlightingConsumer context)
        {
            if (node is IIdentifier)
                context.AddHighlighting(new CgIdentifierHighlighting(HighlightingAttributeIds.LATE_BOUND_IDENTIFIER_ATTRIBUTE, node.GetDocumentRange()));
        }
    }
}