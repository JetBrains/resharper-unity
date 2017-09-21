using JetBrains.Application.Settings;
using JetBrains.ReSharper.Daemon.Stages;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using IArgument = JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Tree.IArgument;
using IFunctionDeclaration = JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Tree.IFunctionDeclaration;
using IIdentifier = JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Tree.IIdentifier;

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

            public override void VisitFieldOperatorNode(IFieldOperator fieldOperatorParam, IHighlightingConsumer context)
            {
                context.AddHighlighting(new CgHighlighting(CgHighlightingAttributeIds.FIELD_IDENTIFIER, fieldOperatorParam.FieldNode.GetDocumentRange()));
                base.VisitFieldOperatorNode(fieldOperatorParam, context);
            }

            public override void VisitPostfixExpressionNode(IPostfixExpression postfixExpressionParam, IHighlightingConsumer context)
            {
                // TODO: fix
                if (postfixExpressionParam.OperatorNode.FirstOrDefault() is ICallOperator
                 && postfixExpressionParam.OperandNode is IIdentifier functionName)
                {
                    context.AddHighlighting(new CgHighlighting(CgHighlightingAttributeIds.FUNCTION_IDENTIFIER, functionName.GetDocumentRange()));
                }
                
                base.VisitPostfixExpressionNode(postfixExpressionParam, context);
            }

            public override void VisitVariableDeclarationNode(IVariableDeclaration variableDeclarationParam, IHighlightingConsumer context)
            {
                context.AddHighlighting(new CgHighlighting(CgHighlightingAttributeIds.TYPE_IDENTIFIER, variableDeclarationParam.TypeNode.GetDocumentRange()));
                HighlightNameNodes(variableDeclarationParam, context, CgHighlightingAttributeIds.VARIABLE_IDENTIFIER);
                base.VisitVariableDeclarationNode(variableDeclarationParam, context);
            }

            public override void VisitBuiltInTypeNode(IBuiltInType builtInTypeParam, IHighlightingConsumer context)
            {
                context.AddHighlighting(new CgHighlighting(CgHighlightingAttributeIds.KEYWORD, builtInTypeParam.GetDocumentRange()));
                base.VisitBuiltInTypeNode(builtInTypeParam, context);
            }

            public override void VisitFieldDeclarationNode(IFieldDeclaration fieldDeclarationParam, IHighlightingConsumer context)
            {
                context.AddHighlighting(new CgHighlighting(CgHighlightingAttributeIds.TYPE_IDENTIFIER, fieldDeclarationParam.ContentNode.TypeNode.GetDocumentRange()));
                HighlightNameNodes(fieldDeclarationParam.ContentNode, context, CgHighlightingAttributeIds.FIELD_IDENTIFIER);
                base.VisitFieldDeclarationNode(fieldDeclarationParam, context);
            }
            
            public override void VisitStructDeclarationNode(IStructDeclaration structDeclarationParam, IHighlightingConsumer context)
            {
                context.AddHighlighting(new CgHighlighting(CgHighlightingAttributeIds.TYPE_IDENTIFIER, structDeclarationParam.GetDocumentRange()));
                base.VisitStructDeclarationNode(structDeclarationParam, context);
            }

            public override void VisitFunctionDeclarationNode(IFunctionDeclaration functionDeclarationParam, IHighlightingConsumer context)
            {
                var header = functionDeclarationParam.HeaderNode;
                context.AddHighlighting(new CgHighlighting(CgHighlightingAttributeIds.TYPE_IDENTIFIER, header.TypeNode.GetDocumentRange()));
                context.AddHighlighting(new CgHighlighting(CgHighlightingAttributeIds.FUNCTION_IDENTIFIER, header.NameNode.GetDocumentRange()));
                base.VisitFunctionDeclarationNode(functionDeclarationParam, context);
            }

            public override void VisitSemanticNode(ISemantic semanticParam, IHighlightingConsumer context)
            {
                context.AddHighlighting(new CgHighlighting(CgHighlightingAttributeIds.KEYWORD, semanticParam.GetDocumentRange())); // TODO: add as proper keywords maybe
                base.VisitSemanticNode(semanticParam, context);
            }

            public override void VisitArgumentNode(IArgument argumentParam, IHighlightingConsumer context)
            {
                context.AddHighlighting(new CgHighlighting(CgHighlightingAttributeIds.TYPE_IDENTIFIER, argumentParam.TypeNode.GetDocumentRange()));
                context.AddHighlighting(new CgHighlighting(CgHighlightingAttributeIds.VARIABLE_IDENTIFIER, argumentParam.NameNode.GetDocumentRange()));
                base.VisitArgumentNode(argumentParam, context);
            }

            public override void VisitFunctionCallNode(IFunctionCall functionCallParam, IHighlightingConsumer context)
            {
                context.AddHighlighting(new CgHighlighting(CgHighlightingAttributeIds.FUNCTION_IDENTIFIER, functionCallParam.NameNode.GetDocumentRange()));
                base.VisitFunctionCallNode(functionCallParam, context);
            }

            public override void VisitCallOperatorNode(ICallOperator callOperatorParam, IHighlightingConsumer context)
            {
                var parent = callOperatorParam.Parent as IPostfixExpression;
                if (parent?.OperandNode is IIdentifier operand) // TODO: this is wrong if this is the constructor of user-declared type
                    context.AddHighlighting(new CgHighlighting(CgHighlightingAttributeIds.FUNCTION_IDENTIFIER, operand.GetDocumentRange()));
                
                base.VisitCallOperatorNode(callOperatorParam, context);
            }

            private void HighlightNameNodes(IVariableDeclaration variableDeclaration, IHighlightingConsumer context, string highlightingAttributeId)
            {
                foreach (var name in variableDeclaration.NameNodes)
                {
                    context.AddHighlighting(new CgHighlighting(highlightingAttributeId, name.GetDocumentRange()));
                }
            }
        }
    }
}