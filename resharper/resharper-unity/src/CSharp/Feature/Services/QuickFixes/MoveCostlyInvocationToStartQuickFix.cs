using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes
{
    [QuickFix]
    public class MoveCostlyInvocationToStartQuickFix : IQuickFix
    {
        private readonly IReference myWarningReference;
        private readonly IInvocationExpression myWarningInvocationExpression;
        private readonly IClassDeclaration myClassDeclaration;
        private readonly bool myWarningIsMoveToStartAvailable;

        public MoveCostlyInvocationToStartQuickFix(PerformanceCriticalCodeInvocationHighlighting warning)
        {
            myWarningReference = warning.Reference;
            myWarningInvocationExpression = warning.InvocationExpression;
            myClassDeclaration = myWarningInvocationExpression?.GetContainingNode<IClassDeclaration>();
            myWarningIsMoveToStartAvailable = warning.IsMoveToStartAvailable;
        }
        public IEnumerable<IntentionAction> CreateBulbItems()
        {
            return new[]
            {
                new IntentionAction(new MoveAction(myClassDeclaration, myWarningInvocationExpression), BulbThemedIcons.ContextAction.Id,
                    IntentionsAnchors.ContextActionsAnchor)
            };
        }

        public bool IsAvailable(IUserDataHolder cache)
        {
            if (!myWarningIsMoveToStartAvailable)
                return false;

            if (myWarningInvocationExpression == null)
                return false;
            
            if (myClassDeclaration == null)
                return false;
            
            if (!myWarningReference.IsValid() || !myWarningInvocationExpression.IsValid() || !myClassDeclaration.IsValid())
                return false;
            
            
            var qualifier = ReferenceExpressionNavigator.GetTopByQualifierExpression(myWarningInvocationExpression?.InvokedExpression as IReferenceExpression)?.QualifierExpression;
            return qualifier == null || MonoBehaviourUtil.IsExpressionAccessibleInScript(qualifier);

        }

        private class MoveAction : BulbActionBase
        {
            private readonly IClassDeclaration myClassDeclaration;
            private readonly IInvocationExpression myInvocationExpression;

            public MoveAction([NotNull] IClassDeclaration classDeclaration,[NotNull] IInvocationExpression invocationExpression)
            {
                myClassDeclaration = classDeclaration;
                myInvocationExpression = invocationExpression;
            }
            
            protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
            {
                MonoBehaviourUtil.MoveToStartWithFieldIntroduction(myClassDeclaration, myInvocationExpression);
                return null;
            }

            public override string Text => "Move to Start";
        }
    }
}