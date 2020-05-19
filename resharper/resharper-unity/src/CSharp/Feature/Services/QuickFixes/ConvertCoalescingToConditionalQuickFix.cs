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
    public class ConvertCoalescingToConditionalQuickFix : UnityScopedQuickFixBase
    {
        private readonly INullCoalescingExpression myExpression;

        public ConvertCoalescingToConditionalQuickFix(UnityObjectNullCoalescingWarning warning)
        {
            myExpression = warning.Expression;
        }

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            using (WriteLockCookie.Create())
            {
                var leftOperand = myExpression.LeftOperand;
                var rightOperand = myExpression.RightOperand;
                var factory = CSharpElementFactory.GetInstance(myExpression);
                var newExpression = factory.CreateExpression("$0?$0:$1", leftOperand, rightOperand);
                ModificationUtil.ReplaceChild(myExpression, newExpression);
            }
            return null;
        }

        protected override ITreeNode TryGetContextTreeNode() => myExpression;
        public override string Text => "Convert to conditional expression";
    }
}
