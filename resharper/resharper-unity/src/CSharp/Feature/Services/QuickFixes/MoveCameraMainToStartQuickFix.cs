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
    public class MoveCameraMainToStartQuickFix : IQuickFix
    {
        private readonly IReferenceExpression myWarningReferenceExpression;
        private readonly IReferenceExpression myFullExpression;
        private readonly IClassDeclaration myClassDeclaration;

        public MoveCameraMainToStartQuickFix(PerformanceCriticalCodeCameraMainHighlighting warning)
        {
            myWarningReferenceExpression = warning.ReferenceExpression.NotNull("warning.ReferenceExpression != null");
            myFullExpression = ReferenceExpressionNavigator.GetTopByQualifierExpression(myWarningReferenceExpression);

            myClassDeclaration = myWarningReferenceExpression.GetContainingNode<IClassDeclaration>();
        }
        public IEnumerable<IntentionAction> CreateBulbItems()
        {
            return new[]
            {
                new IntentionAction(new MoveAction(myClassDeclaration, myFullExpression), BulbThemedIcons.ContextAction.Id,
                    IntentionsAnchors.ContextActionsAnchor)
            };
        }

        public bool IsAvailable(IUserDataHolder cache)
        {
            if (myClassDeclaration == null)
                return false;
            
            if (!myWarningReferenceExpression.IsValid() || !myFullExpression.IsValid() || !myClassDeclaration.IsValid())
                return false;
            
            return myFullExpression.QualifierExpression == null || MonoBehaviourUtil.IsExpressionAccessibleInScript(myFullExpression.QualifierExpression);

        }

        private class MoveAction : BulbActionBase
        {
            private readonly IClassDeclaration myClassDeclaration;
            private readonly ICSharpExpression myExpression;

            public MoveAction([NotNull] IClassDeclaration classDeclaration, [NotNull] ICSharpExpression expression)
            {
                myClassDeclaration = classDeclaration;
                myExpression = expression;
            }
            
            protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
            {
                MonoBehaviourUtil.MoveToStartWithFieldIntroduction(myClassDeclaration, myExpression);
                return null;
            }

            public override string Text => "Move to Start";
        }
    }
}