using JetBrains.Application.Settings;
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
    public class CgIdentifierHighlightingStage : CgDaemonStageBase
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
                context.AddHighlighting(new CgHighlighting(HighlightingAttributeIds.FIELD_IDENTIFIER_ATTRIBUTE, globalVariableDeclarationParam.NameNode.GetDocumentRange()));
                base.VisitGlobalVariableDeclarationNode(globalVariableDeclarationParam, context);
            }

            public override void VisitFieldDeclarationNode(IFieldDeclaration fieldDeclarationParam, IHighlightingConsumer context)
            {
                context.AddHighlighting(new CgHighlighting(HighlightingAttributeIds.FIELD_IDENTIFIER_ATTRIBUTE, fieldDeclarationParam.NameNode.GetDocumentRange()));
                base.VisitFieldDeclarationNode(fieldDeclarationParam, context);
            }
            
            public override void VisitStructDeclarationNode(IStructDeclaration structDeclarationParam, IHighlightingConsumer context)
            {
                context.AddHighlighting(new CgHighlighting(HighlightingAttributeIds.TYPE_STRUCT_ATTRIBUTE, structDeclarationParam.GetDocumentRange()));
                base.VisitStructDeclarationNode(structDeclarationParam, context);
            }

            public override void VisitFunctionDeclarationNode(IFunctionDeclaration functionDeclarationParam, IHighlightingConsumer context)
            {
                context.AddHighlighting(new CgHighlighting(HighlightingAttributeIds.METHOD_IDENTIFIER_ATTRIBUTE, functionDeclarationParam.NameNode.GetDocumentRange()));
                base.VisitFunctionDeclarationNode(functionDeclarationParam, context);
            }

            public override void VisitTypeReferenceNode(ITypeReference typeReferenceParam, IHighlightingConsumer context)
            {
                context.AddHighlighting(new CgHighlighting(HighlightingAttributeIds.TYPE_CLASS_ATTRIBUTE, typeReferenceParam.GetDocumentRange()));
                base.VisitTypeReferenceNode(typeReferenceParam, context);
            }

            public override void VisitSemanticNode(ISemantic semanticParam, IHighlightingConsumer context)
            {
                context.AddHighlighting(new CgHighlighting(HighlightingAttributeIds.KEYWORD, semanticParam.GetDocumentRange())); // TODO: add as proper keywords maybe
                base.VisitSemanticNode(semanticParam, context);
            }

            public override void VisitArgumentNode(IArgument argumentParam, IHighlightingConsumer context)
            {
                context.AddHighlighting(new CgHighlighting(HighlightingAttributeIds.PARAMETER_IDENTIFIER_ATTRIBUTE, argumentParam.NameNode.GetDocumentRange()));
                base.VisitArgumentNode(argumentParam, context);
            }

            public override void VisitConditionalDirectiveNode(IConditionalDirective conditionalDirectiveParam, IHighlightingConsumer context)
            {
                context.AddHighlighting(new CgHighlighting(HighlightingAttributeIds.KEYWORD, conditionalDirectiveParam.HeaderNode.GetDocumentRange()));
                context.AddHighlighting(new CgHighlighting(HighlightingAttributeIds.CPP_MACRO_NAME_ATTRIBUTE, conditionalDirectiveParam.ContentNode.GetDocumentRange()));

                base.VisitConditionalDirectiveNode(conditionalDirectiveParam, context);
            }

            public override void VisitDirectiveNode(IDirective directiveParam, IHighlightingConsumer context)
            {
                context.AddHighlighting(new CgHighlighting(HighlightingAttributeIds.KEYWORD, directiveParam.HeaderNode.GetDocumentRange()));
                context.AddHighlighting(new CgHighlighting(HighlightingAttributeIds.CPP_MACRO_NAME_ATTRIBUTE, directiveParam.ContentNode.GetDocumentRange()));

                base.VisitDirectiveNode(directiveParam, context);
            }

            public override void VisitConditionalDirectiveFooterNode(IConditionalDirectiveFooter conditionalDirectiveFooterParam,
                IHighlightingConsumer context)
            {
                context.AddHighlighting(new CgHighlighting(HighlightingAttributeIds.KEYWORD, conditionalDirectiveFooterParam.HeaderNode.GetDocumentRange()));
                context.AddHighlighting(new CgHighlighting(HighlightingAttributeIds.CPP_MACRO_NAME_ATTRIBUTE, conditionalDirectiveFooterParam.ContentNode.GetDocumentRange()));

                base.VisitConditionalDirectiveFooterNode(conditionalDirectiveFooterParam, context);
            }

            public override void VisitLocalVariableDeclarationNode(ILocalVariableDeclaration localVariableDeclarationParam,
                IHighlightingConsumer context)
            {
                context.AddHighlighting(new CgHighlighting(HighlightingAttributeIds.LOCAL_VARIABLE_IDENTIFIER_ATTRIBUTE, localVariableDeclarationParam.NameNode.GetDocumentRange()));
                base.VisitLocalVariableDeclarationNode(localVariableDeclarationParam, context);
            }

            public override void VisitVariableReferenceNode(IVariableReference variableReferenceParam, IHighlightingConsumer context)
            {
                context.AddHighlighting(new CgHighlighting(HighlightingAttributeIds.LOCAL_VARIABLE_IDENTIFIER_ATTRIBUTE, variableReferenceParam.GetDocumentRange()));
                base.VisitVariableReferenceNode(variableReferenceParam, context);
            }

            public override void VisitFieldNameNode(IFieldName fieldNameParam, IHighlightingConsumer context)
            {
                context.AddHighlighting(new CgHighlighting(HighlightingAttributeIds.FIELD_IDENTIFIER_ATTRIBUTE, fieldNameParam.GetDocumentRange()));
                base.VisitFieldNameNode(fieldNameParam, context);
            }
        }
    }
}