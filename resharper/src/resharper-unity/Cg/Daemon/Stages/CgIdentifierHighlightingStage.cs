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
            return new IdentifierHighlightingProcess(process, settings, file);
        }

        private class IdentifierHighlightingProcess : CgDaemonStageProcessBase
        {
            public IdentifierHighlightingProcess(IDaemonProcess daemonProcess, IContextBoundSettingsStore settingsStore, ICgFile file)
                : base(daemonProcess, settingsStore, file)
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
                context.AddHighlighting(new CgHighlighting(CgHighlightingAttributeIds.TYPE_IDENTIFIER, structDeclarationParam.GetDocumentRange()));
                base.VisitStructDeclarationNode(structDeclarationParam, context);
            }

            public override void VisitFunctionDeclarationNode(IFunctionDeclaration functionDeclarationParam, IHighlightingConsumer context)
            {
                context.AddHighlighting(new CgHighlighting(CgHighlightingAttributeIds.METHOD_IDENTIFIER, functionDeclarationParam.NameNode.GetDocumentRange()));
                base.VisitFunctionDeclarationNode(functionDeclarationParam, context);
            }

            public override void VisitTypeReferenceNode(ITypeReference typeReferenceParam, IHighlightingConsumer context)
            {
                context.AddHighlighting(new CgHighlighting(CgHighlightingAttributeIds.TYPE_IDENTIFIER, typeReferenceParam.GetDocumentRange()));
                base.VisitTypeReferenceNode(typeReferenceParam, context);
            }

            public override void VisitSemanticNode(ISemantic semanticParam, IHighlightingConsumer context)
            {
                context.AddHighlighting(new CgHighlighting(CgHighlightingAttributeIds.KEYWORD, semanticParam.GetDocumentRange())); // TODO: add as proper keywords maybe
                base.VisitSemanticNode(semanticParam, context);
            }

            public override void VisitArgumentNode(IArgument argumentParam, IHighlightingConsumer context)
            {
                context.AddHighlighting(new CgHighlighting(CgHighlightingAttributeIds.VARIABLE_IDENTIFIER, argumentParam.NameNode.GetDocumentRange()));
                base.VisitArgumentNode(argumentParam, context);
            }

            public override void VisitLocalVariableDeclarationNode(ILocalVariableDeclaration localVariableDeclarationParam,
                IHighlightingConsumer context)
            {
                context.AddHighlighting(new CgHighlighting(CgHighlightingAttributeIds.VARIABLE_IDENTIFIER, localVariableDeclarationParam.NameNode.GetDocumentRange()));
                base.VisitLocalVariableDeclarationNode(localVariableDeclarationParam, context);
            }

            public override void VisitVariableReferenceNode(IVariableReference variableReferenceParam, IHighlightingConsumer context)
            {
                context.AddHighlighting(new CgHighlighting(CgHighlightingAttributeIds.VARIABLE_IDENTIFIER, variableReferenceParam.GetDocumentRange()));
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
        }
    }
}