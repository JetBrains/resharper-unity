using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Progress;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Analyzers;
using JetBrains.ReSharper.Plugins.Unity.Resources;
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
        private readonly ICSharpExpression myExpression;
        private List<ICSharpExpression> myScalars;
        private List<ICSharpExpression> myMatrices;

        public MultiplicationOrderQuickFix(InefficientMultiplicationOrderWarning warning)
        {
            myExpression = warning.Expression;
            myScalars = warning.Scalars;
            myMatrices = warning.Matrices;
        }
        
        private const string mul = "$0 * $1";

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            var factory = CSharpElementFactory.GetInstance(myExpression);

            var newScalars = myScalars.Select(t => t.CopyWithResolve()).ToList();
            var newExpression = RemoveScalars(factory, myExpression);
            
            var scalars = CreateMulExpression(factory, newScalars);
            myExpression.ReplaceBy(factory.CreateExpression(mul, newExpression, scalars));
            
            return null;
        }

        private ICSharpExpression RemoveScalars(CSharpElementFactory elementFactory, ICSharpExpression expression)
        {
            if (!MultiplicationOrderAnalyzer.IsMatrixType(expression))
                return null;

            var multiplication = MultiplicationOrderAnalyzer.GetMulOperation(expression.GetOperandThroughParenthesis());
            if (multiplication == null)
                return expression;

            var left = RemoveScalars(elementFactory, multiplication.LeftOperand);
            var right = RemoveScalars(elementFactory, multiplication.RightOperand);

            if (left == null)
            {
                Assertion.Assert(right != null, "right != null");
                return right;
            }

            if (right == null)
            {
                Assertion.Assert(left != null, "left != null");
                return left;
            }

            return elementFactory.CreateExpression(mul, left.CopyWithResolve(), right.CopyWithResolve());
        }

        private ICSharpExpression CreateMulExpression(CSharpElementFactory factory, List<ICSharpExpression> elements)
        {
            if (elements.Count == 1)
                return elements[0].CopyWithResolve();
            
            var newExpr = factory.CreateExpression(mul, elements[0].CopyWithResolve(), elements[1].CopyWithResolve());
            for (int i = 2; i < elements.Count; i++)
                newExpr = factory.CreateExpression(mul, newExpr, elements[i].CopyWithResolve());

            return newExpr;
        }
        
        public override bool IsAvailable(IUserDataHolder cache) => myExpression.IsValid();

        public override string Text => Strings.MultiplicationOrderQuickFix_Text_Reorder_multiplication;

    }
}