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
    public class  MultiplicationOrderQuickFix : QuickFixBase
    {
        private readonly IMultiplicativeExpression myExpression;

        public MultiplicationOrderQuickFix(InefficientMultiplicationOrderWarning warning)
        {
            myExpression = warning.Expression;
        }
        
        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            var operands = GetAllOperands(myExpression).OrderBy(t => !t.GetExpressionType().ToIType().IsPredefinedNumeric()).ToList();
            var factory = CSharpElementFactory.GetInstance(myExpression);

            const string mul = "$0 * $1";
            var newExpr = operands[0];
            for (int i = 1; i < operands.Count; i++)
                newExpr = factory.CreateExpression(mul, newExpr, operands[i].CopyWithResolve());
           
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

        public override string Text => "Reorder multiplication";

    }
}