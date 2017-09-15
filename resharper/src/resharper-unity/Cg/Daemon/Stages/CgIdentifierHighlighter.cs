using JetBrains.ReSharper.Feature.Services.CSharp.CompleteStatement;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Tree;
using JetBrains.ReSharper.Psi.Tree;
using IArgument = JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Tree.IArgument;
using IFunctionDeclaration = JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Tree.IFunctionDeclaration;

namespace JetBrains.ReSharper.Plugins.Unity.Cg.Daemon.Stages
{
    public class CgIdentifierHighlighter
    {
        public void Highlight(ITreeNode node, IHighlightingConsumer context)
        {
            // don't process a lot of things inside of a conditional directive
//            if (node.Parent is IDirectiveInternalContent)
//            {
//                if (!(node is IDirective))
//                    return;
//            }

            // TODO: separate to different stages
            // also for proper value reference highligthing we'll need to resolve references to differentiate between field, variable and argument
            
            switch (node)
            {
                case IErrorElement e: // TODO: probably disable until we have proper preprocessor support
                    context.AddHighlighting(new CgHighlight52ing(HighlightingAttributeIds.ERROR_ATTRIBUTE, e.GetDocumentRange()));
                    break;
            }
        }
    }
}