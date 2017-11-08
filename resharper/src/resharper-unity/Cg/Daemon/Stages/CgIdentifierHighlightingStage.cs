using System;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Daemon.Stages;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using IFunctionDeclaration = JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Tree.IFunctionDeclaration;
using IIdentifier = JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Tree.IIdentifier;

namespace JetBrains.ReSharper.Plugins.Unity.Cg.Daemon.Stages
{
    [DaemonStage(StagesBefore = new[] { typeof(GlobalFileStructureCollectorStage) },
        StagesAfter = new [] { typeof(CollectUsagesStage)} )]
    public class CgIdentifierHighlightingStage : CgDaemonStageBase
    {
        private readonly ILogger myLogger;

        public CgIdentifierHighlightingStage(ILogger logger)
        {
            myLogger = logger;
        }
        
        protected override IDaemonStageProcess CreateProcess(
            IDaemonProcess process, IContextBoundSettingsStore settings,
            DaemonProcessKind processKind, ICgFile file)
        {
            return new IdentifierHighlightingProcess(myLogger, process, file);
        }

        private class IdentifierHighlightingProcess : CgDaemonStageProcessBase
        {
            private readonly ILogger myLogger;
            
            public IdentifierHighlightingProcess(ILogger logger, IDaemonProcess daemonProcess, ICgFile file)
                : base(daemonProcess, file)
            {
                myLogger = logger;
            }

            public override void VisitFieldOperatorNode(IFieldOperator fieldOperatorParam, IHighlightingConsumer context)
            {
                context.AddHighlighting(new CgHighlighting(CgHighlightingAttributeIds.FIELD_IDENTIFIER, fieldOperatorParam.FieldNode.GetDocumentRange()));
                base.VisitFieldOperatorNode(fieldOperatorParam, context);
            }

            public override void VisitPostfixExpressionNode(IPostfixExpression postfixExpressionParam, IHighlightingConsumer context)
            {
                if (postfixExpressionParam.OperatorNode.FirstOrDefault() is ICallOperator
                 && postfixExpressionParam.OperandNode is IIdentifier functionName)
                {
                    context.AddHighlighting(new CgHighlighting(CgHighlightingAttributeIds.FUNCTION_IDENTIFIER, functionName.GetDocumentRange()));
                }
                
                base.VisitPostfixExpressionNode(postfixExpressionParam, context);
            }

            public override void VisitSingleVariableDeclarationNode(ISingleVariableDeclaration singleVariableDeclarationParam,
                IHighlightingConsumer context)
            {
                var typeName = singleVariableDeclarationParam.TypeNode;
                if (typeName is IIdentifier userDeclaredType)
                {
                    context.AddHighlighting(new CgHighlighting(CgHighlightingAttributeIds.TYPE_IDENTIFIER, userDeclaredType.GetDocumentRange()));
                }
                
                base.VisitSingleVariableDeclarationNode(singleVariableDeclarationParam, context);
            }

            public override void VisitVariableDeclarationNode(IVariableDeclaration variableDeclarationParam, IHighlightingConsumer context)
            {
                HighlightNameNodes(variableDeclarationParam, context, CgHighlightingAttributeIds.VARIABLE_IDENTIFIER);
                base.VisitVariableDeclarationNode(variableDeclarationParam, context);
            }

            public override void VisitFieldDeclarationNode(IFieldDeclaration fieldDeclarationParam, IHighlightingConsumer context)
            {
                var variableDeclaration = fieldDeclarationParam.ContentNode;
                if (variableDeclaration != null)
                {
                    HighlightNameNodes(variableDeclaration, context, CgHighlightingAttributeIds.FIELD_IDENTIFIER);
                }
                
                base.VisitFieldDeclarationNode(fieldDeclarationParam, context);
            }
            
            public override void VisitStructDeclarationNode(IStructDeclaration structDeclarationParam, IHighlightingConsumer context)
            {
                context.AddHighlighting(new CgHighlighting(CgHighlightingAttributeIds.TYPE_IDENTIFIER, structDeclarationParam.NameNode.GetDocumentRange()));
                base.VisitStructDeclarationNode(structDeclarationParam, context);
            }

            public override void VisitFunctionDeclarationNode(IFunctionDeclaration functionDeclarationParam, IHighlightingConsumer context)
            {
                var header = functionDeclarationParam.HeaderNode;
                if (header.TypeNode is IIdentifier userDeclaredType)
                    context.AddHighlighting(new CgHighlighting(CgHighlightingAttributeIds.TYPE_IDENTIFIER, userDeclaredType.GetDocumentRange()));

                try
                {
                    context.AddHighlighting(new CgHighlighting(CgHighlightingAttributeIds.FUNCTION_IDENTIFIER,
                        header.NameNode.GetDocumentRange()));
                }
                catch (InvalidCastException ex)
                {
                    // TODO: remove after PP implementation
                    myLogger.LogExceptionSilently(ex);
                }

                base.VisitFunctionDeclarationNode(functionDeclarationParam, context);
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