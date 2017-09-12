using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Psi.Cg.Tree;
using JetBrains.ReSharper.Psi.Tree;
using IArgument = JetBrains.ReSharper.Plugins.Unity.Psi.Cg.Tree.IArgument;
using IFunctionDeclaration = JetBrains.ReSharper.Plugins.Unity.Psi.Cg.Tree.IFunctionDeclaration;

namespace JetBrains.ReSharper.Plugins.Unity.Daemon.Cg.Stages
{
    public class CgIdentifierHighlighter
    {
        public void Highlight(ITreeNode node, IHighlightingConsumer context)
        {
            // TODO: refactor
            // for proper value reference highligthing we'll need to resolve references to differentiate between field, variable and argument
            
            if (node is ITypeReference)
                context.AddHighlighting(new CgIdentifierHighlighting(HighlightingAttributeIds.TYPE_CLASS_ATTRIBUTE, node.GetDocumentRange()));
            else if (node is ISemantic)
                context.AddHighlighting(new CgIdentifierHighlighting(HighlightingAttributeIds.KEYWORD, node.GetDocumentRange())); // TODO: add as proper keywords maybe
            else if (node is IGlobalVariableDeclaration)
                context.AddHighlighting(new CgIdentifierHighlighting(HighlightingAttributeIds.FIELD_IDENTIFIER_ATTRIBUTE, ((IGlobalVariableDeclaration) node).NameNode.GetDocumentRange()));
            else if (node is ILocalVariableDeclaration)
                context.AddHighlighting(new CgIdentifierHighlighting(HighlightingAttributeIds.LOCAL_VARIABLE_IDENTIFIER_ATTRIBUTE, ((ILocalVariableDeclaration) node).NameNode.GetDocumentRange()));
            else if (node is IArgument)
                context.AddHighlighting(new CgIdentifierHighlighting(HighlightingAttributeIds.PARAMETER_IDENTIFIER_ATTRIBUTE, ((IArgument)node).NameNode.GetDocumentRange()));
            else if (node is IFunctionDeclaration)
                context.AddHighlighting(new CgIdentifierHighlighting(HighlightingAttributeIds.METHOD_IDENTIFIER_ATTRIBUTE, ((IFunctionDeclaration)node).NameNode.GetDocumentRange()));
        }
    }
}