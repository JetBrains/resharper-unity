using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Parsing;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(ICSharpExpression), HighlightingTypes =
        new[] {typeof(InefficientMultiplicationOrderWarning)})]
    public class MultiplicationOrderAnalyzer : UnityElementProblemAnalyzer<ICSharpExpression>
    {
        private static Dictionary<IClrTypeName, int> knownTypes = new Dictionary<IClrTypeName, int>()
        {
            {new ClrTypeName("UnityEngine.Vector2"), 2},
            {new ClrTypeName("UnityEngine.Vector3"), 3},
            {new ClrTypeName("UnityEngine.Vector4"), 4},
            {new ClrTypeName("UnityEngine.Vector2Int"), 2},
            {new ClrTypeName("UnityEngine.Vector3Int"), 3},
        };

        public MultiplicationOrderAnalyzer(UnityApi unityApi)
            : base(unityApi)
        {
        }

        protected override void Analyze(ICSharpExpression expression, ElementProblemAnalyzerData data,
            IHighlightingConsumer consumer)
        {

            var parent = expression.GetContainingParenthesizedExpression()?.Parent as ICSharpExpression;
            if (GetMulOperation(expression) != null || GetMulOperation(parent) == null)
                return;
            
            if (IsMatrixType(expression.GetExpressionType()))
            {
                var count = 0;
                ICSharpExpression scalar = null;
                while (true)
                {
                    count++;
                    var curExpr = parent.GetContainingParenthesizedExpression();
                    var byLeft = MultiplicativeExpressionNavigator.GetByLeftOperand(curExpr);
                    var byRight = MultiplicativeExpressionNavigator.GetByRightOperand(curExpr);
                    if (byLeft == null && byRight != null)
                    {
                        scalar = byRight?.LeftOperand;
                        parent = byRight;
                    } else if (byRight == null && byLeft != null)
                    {
                        scalar = byLeft?.RightOperand;
                        parent = byLeft;
                    }
                    else
                    {
                        break;
                    }
                }
                
                // incomplete expression
                if (scalar == null)
                    return;
                
                if (count > 1)
                {
                    Assertion.Assert(scalar != null, "scalar != null");
                    consumer.AddHighlighting(new InefficientMultiplicationOrderWarning(parent, expression, scalar));
                }
            }
        }

        private IMultiplicativeExpression GetMulOperation(ICSharpExpression expression)
        {
            if (expression is IMultiplicativeExpression mul && mul.OperatorSign.GetTokenType() == CSharpTokenType.ASTERISK)
                return mul;
            
            return null;
        }

        private int GetMulCount(ICSharpExpression expression)
        {
            if (expression is IMultiplicativeExpression mul &&
                mul.OperatorSign.GetTokenType() == CSharpTokenType.ASTERISK)
            {
                return GetMulCount(mul.LeftOperand) + GetMulCount(mul.RightOperand);
            }
            else
            {
                return 1;
            }
        }

        private bool IsMatrixType([NotNull] IExpressionType expression)
        {
            var clrType = (expression as IDeclaredType)?.GetClrName();
            if (clrType == null)
                return false;
            return knownTypes.ContainsKey(clrType);
        }
    }
}