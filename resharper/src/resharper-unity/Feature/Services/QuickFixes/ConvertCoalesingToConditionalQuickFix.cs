using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Intentions.Util;
using JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Feature.Services.QuickFixes
{
    [QuickFix]
    public class ConvertCoalesingToConditionalQuickFix : QuickFixBase
    {
        private readonly INullCoalescingExpression myExpression;

        public ConvertCoalesingToConditionalQuickFix(UnityNullCoalescingWarning warning)
        {
            myExpression = warning.Expression;
        }

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            using (WriteLockCookie.Create())
            {
                var leftOperand = myExpression.LeftOperand;
                var rightOperand = myExpression.RightOperand;
                if (leftOperand == null || rightOperand == null)
                    return null;
                var factory = CSharpElementFactory.GetInstance(myExpression);
                var newExpression = factory.CreateExpression("$0?$0:$1", leftOperand, rightOperand);
                ModificationUtil.ReplaceChild(myExpression, newExpression);
            }
            return null;
        }

        public override string Text => "Convert to conditional expression.";

        public override bool IsAvailable(IUserDataHolder cache)
        {
            return ValidUtils.Valid(myExpression);
        }
    }
}
