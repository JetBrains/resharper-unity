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
            // TODO: separate to different stages
            // also for proper value reference highligthing we'll need to resolve references to differentiate between field, variable and argument
            
            switch (node)
            {
                case IErrorElement e: // TODO: probably disable until we have proper preprocessor support
                    context.AddHighlighting(new CgIdentifierHighlighting(HighlightingAttributeIds.ERROR_ATTRIBUTE, e.GetDocumentRange()));
                    break;
                case ITypeReference t:
                    context.AddHighlighting(new CgIdentifierHighlighting(HighlightingAttributeIds.TYPE_CLASS_ATTRIBUTE, t.GetDocumentRange()));
                    break;
                case ISemantic s:
                    context.AddHighlighting(new CgIdentifierHighlighting(HighlightingAttributeIds.KEYWORD, s.GetDocumentRange())); // TODO: add as proper keywords maybe
                    break;
                case IGlobalVariableDeclaration v:
                    context.AddHighlighting(new CgIdentifierHighlighting(HighlightingAttributeIds.FIELD_IDENTIFIER_ATTRIBUTE, v.NameNode.GetDocumentRange()));
                    break;
                case ILocalVariableDeclaration v:
                    context.AddHighlighting(new CgIdentifierHighlighting(HighlightingAttributeIds.LOCAL_VARIABLE_IDENTIFIER_ATTRIBUTE, v.NameNode.GetDocumentRange()));
                    break;
                case IArgument a:
                    context.AddHighlighting(new CgIdentifierHighlighting(HighlightingAttributeIds.PARAMETER_IDENTIFIER_ATTRIBUTE, a.NameNode.GetDocumentRange()));
                    break;
                case IFunctionDeclaration f:
                    context.AddHighlighting(new CgIdentifierHighlighting(HighlightingAttributeIds.METHOD_IDENTIFIER_ATTRIBUTE, f.NameNode.GetDocumentRange()));
                    break;
            }
        }
    }
}