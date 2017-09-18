using JetBrains.Application.Settings;
using JetBrains.ReSharper.Cg.Daemon.Errors;
using JetBrains.ReSharper.Daemon.Stages;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Tree;
using JetBrains.ReSharper.Psi.Tree;
using IArgument = JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Tree.IArgument;
using IFunctionDeclaration = JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Tree.IFunctionDeclaration;

namespace JetBrains.ReSharper.Plugins.Unity.Cg.Daemon.Stages
{
    [DaemonStage(StagesBefore = new[] { typeof(GlobalFileStructureCollectorStage) },
        StagesAfter = new [] { typeof(CollectUsagesStage)} )]
    public class CgSyntaxHighlightingStage : CgDaemonStageBase
    {
        protected override IDaemonStageProcess CreateProcess(IDaemonProcess process, IContextBoundSettingsStore settings,
            DaemonProcessKind processKind, ICgFile file)
        {
            return new IdentifierHighlightingProcess(process, file, settings);
        }

        private class IdentifierHighlightingProcess : CgDaemonStageProcessBase
        {
            public IdentifierHighlightingProcess(IDaemonProcess daemonProcess, ICgFile file, IContextBoundSettingsStore settingsStore)
                : base(daemonProcess, file, settingsStore)
            {
            }

            public override void VisitGlobalVariableDeclarationNode(IGlobalVariableDeclaration globalVariableDeclarationParam,
                IHighlightingConsumer context)
            {
                context.AddHighlighting(new CgHighlighting(CgHighlightingAttributeIds.FIELD_IDENTIFIER, globalVariableDeclarationParam.NameNode.GetDocumentRange()));
                base.VisitGlobalVariableDeclarationNode(globalVariableDeclarationParam, context);
            }

            public override void VisitFieldDeclarationNode(IFieldDeclaration fieldDeclarationParam, IHighlightingConsumer context)
            {
                context.AddHighlighting(new CgHighlighting(CgHighlightingAttributeIds.FIELD_IDENTIFIER, fieldDeclarationParam.NameNode.GetDocumentRange()));
                base.VisitFieldDeclarationNode(fieldDeclarationParam, context);
            }
            
            public override void VisitStructDeclarationNode(IStructDeclaration structDeclarationParam, IHighlightingConsumer context)
            {
                context.AddHighlighting(new CgHighlighting(CgHighlightingAttributeIds.TYPE_STRUCT, structDeclarationParam.GetDocumentRange()));
                base.VisitStructDeclarationNode(structDeclarationParam, context);
            }

            public override void VisitFunctionDeclarationNode(IFunctionDeclaration functionDeclarationParam, IHighlightingConsumer context)
            {
                context.AddHighlighting(new CgHighlighting(CgHighlightingAttributeIds.METHOD_IDENTIFIER, functionDeclarationParam.NameNode.GetDocumentRange()));
                base.VisitFunctionDeclarationNode(functionDeclarationParam, context);
            }

            public override void VisitTypeReferenceNode(ITypeReference typeReferenceParam, IHighlightingConsumer context)
            {
                context.AddHighlighting(new CgHighlighting(CgHighlightingAttributeIds.TYPE_CLASS, typeReferenceParam.GetDocumentRange()));
                base.VisitTypeReferenceNode(typeReferenceParam, context);
            }

            public override void VisitSemanticNode(ISemantic semanticParam, IHighlightingConsumer context)
            {
                context.AddHighlighting(new CgHighlighting(CgHighlightingAttributeIds.KEYWORD, semanticParam.GetDocumentRange())); // TODO: add as proper keywords maybe
                base.VisitSemanticNode(semanticParam, context);
            }

            public override void VisitArgumentNode(IArgument argumentParam, IHighlightingConsumer context)
            {
                context.AddHighlighting(new CgHighlighting(CgHighlightingAttributeIds.PARAMETER_IDENTIFIER, argumentParam.NameNode.GetDocumentRange()));
                base.VisitArgumentNode(argumentParam, context);
            }

            public override void VisitConditionalDirectiveNode(IConditionalDirective conditionalDirectiveParam, IHighlightingConsumer context)
            {
                context.AddHighlighting(new CgHighlighting(CgHighlightingAttributeIds.KEYWORD, conditionalDirectiveParam.HeaderNode.GetDocumentRange()));
                context.AddHighlighting(new CgHighlighting(CgHighlightingAttributeIds.CPP_MACRO_NAME, conditionalDirectiveParam.ContentNode.GetDocumentRange()));

                base.VisitConditionalDirectiveNode(conditionalDirectiveParam, context);
            }

            public override void VisitDirectiveNode(IDirective directiveParam, IHighlightingConsumer context)
            {
                context.AddHighlighting(new CgHighlighting(CgHighlightingAttributeIds.KEYWORD, directiveParam.HeaderNode.GetDocumentRange()));
                context.AddHighlighting(new CgHighlighting(CgHighlightingAttributeIds.CPP_MACRO_NAME, directiveParam.ContentNode.GetDocumentRange()));

                base.VisitDirectiveNode(directiveParam, context);
            }

            public override void VisitConditionalDirectiveFooterNode(IConditionalDirectiveFooter conditionalDirectiveFooterParam,
                IHighlightingConsumer context)
            {
                context.AddHighlighting(new CgHighlighting(CgHighlightingAttributeIds.KEYWORD, conditionalDirectiveFooterParam.HeaderNode.GetDocumentRange()));
                context.AddHighlighting(new CgHighlighting(CgHighlightingAttributeIds.CPP_MACRO_NAME, conditionalDirectiveFooterParam.ContentNode.GetDocumentRange()));

                base.VisitConditionalDirectiveFooterNode(conditionalDirectiveFooterParam, context);
            }

            public override void VisitLocalVariableDeclarationNode(ILocalVariableDeclaration localVariableDeclarationParam,
                IHighlightingConsumer context)
            {
                context.AddHighlighting(new CgHighlighting(CgHighlightingAttributeIds.LOCAL_VARIABLE_IDENTIFIER, localVariableDeclarationParam.NameNode.GetDocumentRange()));
                base.VisitLocalVariableDeclarationNode(localVariableDeclarationParam, context);
            }

            public override void VisitVariableReferenceNode(IVariableReference variableReferenceParam, IHighlightingConsumer context)
            {
                context.AddHighlighting(new CgHighlighting(CgHighlightingAttributeIds.LOCAL_VARIABLE_IDENTIFIER, variableReferenceParam.GetDocumentRange()));
                base.VisitVariableReferenceNode(variableReferenceParam, context);
            }

            public override void VisitFieldNameNode(IFieldName fieldNameParam, IHighlightingConsumer context)
            {
                context.AddHighlighting(new CgHighlighting(CgHighlightingAttributeIds.FIELD_IDENTIFIER, fieldNameParam.GetDocumentRange()));
                base.VisitFieldNameNode(fieldNameParam, context);
            }

            public override void VisitFunctionCallNode(IFunctionCall functionCallParam, IHighlightingConsumer context)
            {
                context.AddHighlighting(new CgHighlighting(CgHighlightingAttributeIds.METHOD_IDENTIFIER, functionCallParam.NameNode.GetDocumentRange()));
                base.VisitFunctionCallNode(functionCallParam, context);
            }

            public override void VisitConstantValueNode(IConstantValue constantValueParam, IHighlightingConsumer context)
            {
                context.AddHighlighting(new CgHighlighting(CgHighlightingAttributeIds.NUMBER, constantValueParam.GetDocumentRange()));
                base.VisitConstantValueNode(constantValueParam, context);
            }

#if DEBUG
            public override void VisitNode(ITreeNode node, IHighlightingConsumer context)
            {
                if (node is IErrorElement errorElement)
                {
                    var range = errorElement.GetDocumentRange();
                    if (!range.IsValid())
                        range = node.Parent.GetDocumentRange();
                    if (range.TextRange.IsEmpty)
                    {
                        if (range.TextRange.EndOffset < range.Document.GetTextLength())
                            range = range.ExtendRight(1);
                        else
                            range = range.ExtendLeft(1);
                    }
                    context.AddHighlighting(new CgSyntaxError(errorElement.ErrorDescription, range), range);
                }
                
                base.VisitNode(node, context);
            }
            #endif
        }
    }
}