using System;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes
{
    [QuickFix]
    public class ConvertToCompareTagQuickFix : UnityScopedQuickFixBase
    {
        private readonly IEqualityExpression myExpression;
        private readonly bool myRewriteLeftOperand;

        public ConvertToCompareTagQuickFix(ExplicitTagStringComparisonWarning warning)
        {
            myExpression = warning.EqualityExpression;
            myRewriteLeftOperand = warning.LeftOperandIsTagReference;
        }

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            using (WriteLockCookie.Create())
            {
                var qualifierOperand =
                    (myRewriteLeftOperand ? myExpression.LeftOperand : myExpression.RightOperand) as
                    IReferenceExpression;
                var qualifierExpression = qualifierOperand?.QualifierExpression;
                var otherOperand = myRewriteLeftOperand ? myExpression.RightOperand : myExpression.LeftOperand;
                var factory = CSharpElementFactory.GetInstance(myExpression);
                ICSharpExpression newExpression;
                if (qualifierExpression != null)
                {
                    newExpression = factory.CreateExpression("$0$1.CompareTag($2)",
                        myExpression.EqualityType == EqualityExpressionType.EQEQ ? string.Empty : "!",
                        qualifierExpression, otherOperand);
                }
                else
                {
                    newExpression = factory.CreateExpression("$0CompareTag($1)",
                        myExpression.EqualityType == EqualityExpressionType.EQEQ ? string.Empty : "!",
                        otherOperand);
                }

                ModificationUtil.ReplaceChild(myExpression, newExpression);
            }

            return null;
        }

        public override string Text => "Convert to 'CompareTag'";
        protected override ITreeNode TryGetContextTreeNode() => myExpression;
    }
}