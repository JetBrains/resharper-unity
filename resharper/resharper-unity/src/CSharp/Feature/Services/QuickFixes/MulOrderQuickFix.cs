using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes
{
    [QuickFix]
    public class MulOrderQuickFix : QuickFixBase
    {
        private readonly IMultiplicativeExpression myExpression;

        public MulOrderQuickFix(InefficientMultiplyOrderWarning warning)
        {
            myExpression = warning.Expression;
        }
        
        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            var operands = GetAllOperands(myExpression);

            var numerics = operands.Where(t => t.GetExpressionType().ToIType().IsPredefinedNumeric()).ToList();
            var vector = operands.Single(t => !t.GetExpressionType().ToIType().IsPredefinedNumeric());

            var factory = CSharpElementFactory.GetInstance(myExpression);

            const string mul = "$0 * $1";
            var newExpr = factory.CreateExpression(mul, numerics[0].CopyWithResolve(), numerics[1].CopyWithResolve());
            for (int i = 2; i < numerics.Count; i++)
                newExpr = factory.CreateExpression(mul, newExpr, numerics[i].CopyWithResolve());

            newExpr = factory.CreateExpression(mul, newExpr, vector.CopyWithResolve());
            myExpression.ReplaceBy(newExpr);
            
            return null;
        }

        private List<ICSharpExpression> GetAllOperands(IMultiplicativeExpression expression)
        {
            var result = new List<ICSharpExpression>();
            var left = expression.LeftOperand.GetOperandThroughParenthesis();
            var right = expression.RightOperand.GetOperandThroughParenthesis();

            if (left is IMultiplicativeExpression leftM)
            {
                result.AddRange(GetAllOperands(leftM));
            }
            else
            {
                result.Add(left);
            }
            
            if (right is IMultiplicativeExpression rightM)
            {
                result.AddRange(GetAllOperands(rightM));
            }
            else
            {
                result.Add(right);
            }

            return result;
        }

        public override bool IsAvailable(IUserDataHolder cache) => myExpression.IsValid();

        public override string Text => "Reorder operations";

    }
}