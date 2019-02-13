using     System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(IMultiplicativeExpression), HighlightingTypes =
        new[] {typeof(InefficientMultiplicationOrderWarning)})]
    public class MultiplicationOrderAnalyzer : UnityElementProblemAnalyzer<IMultiplicativeExpression>
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

        protected override void Analyze(IMultiplicativeExpression expression, ElementProblemAnalyzerData data,
            IHighlightingConsumer consumer)
        {
            var byLeft = MultiplicativeExpressionNavigator.GetByLeftOperand(expression.GetContainingParenthesizedExpression());
            if (byLeft != null)
                return;

            var byRight = MultiplicativeExpressionNavigator.GetByRightOperand(expression.GetContainingParenthesizedExpression());
            if (byRight != null)
                return;


            var (count, hasUnknownType) = CalculateMatrixIntMulCount(expression);
            if (hasUnknownType)
                return;

            if (count > GetElementCount(expression.GetExpressionType()))
                consumer.AddHighlighting(new InefficientMultiplicationOrderWarning(expression));
        }

        private (int, bool) CalculateMatrixIntMulCount(ICSharpExpression expression)
        {
            if (expression == null)
                return (0, false);

            if (!(expression is IBinaryExpression binaryExpression))
                return (0, false);

            var left = binaryExpression.LeftOperand.GetOperandThroughParenthesis();
            var right = binaryExpression.RightOperand.GetOperandThroughParenthesis();
            if (left == null || right == null)
                return (0, true);

            var leftType = left.GetExpressionType();
            var rightType = right.GetExpressionType();
            if (leftType.IsUnknown || rightType.IsUnknown)
                return (0, true);

            if (!IsAcceptableType(leftType) || !IsAcceptableType(rightType))
                return (0, true);

            var leftMulCount = 0;
            var rightMulCount = 0;
            if (IsMatrixType(leftType))
            {
                var (newCount, hasUnknownType) = CalculateMatrixIntMulCount(left);
                if (hasUnknownType)
                    return (0, true);

                leftMulCount += newCount;
            }

            if (IsMatrixType(rightType))
            {
                var (newCount, hasUnknownType) = CalculateMatrixIntMulCount(right);
                if (hasUnknownType)
                    return (0, true);
                rightMulCount += newCount;
            }

            var mulCount = 0;

            if (IsMatrixType(leftType) && right.GetExpressionType().ToIType().IsPredefinedNumeric())
            {
                mulCount += GetElementCount(leftType);
            }

            if (IsMatrixType(rightType) && left.GetExpressionType().ToIType().IsPredefinedNumeric())
            {
                mulCount += GetElementCount(rightType);
            }
            
            return (leftMulCount + rightMulCount + mulCount, false);
        }


        private bool IsAcceptableType(IExpressionType rightType)
        {
            return IsMatrixType(rightType) || rightType.ToIType()?.IsPredefinedNumeric() == true;
        }

        private bool IsMatrixType([NotNull] IExpressionType expression)
        {
            var clrType = (expression as IDeclaredType)?.GetClrName();
            if (clrType == null)
                return false;
            return knownTypes.ContainsKey(clrType);
        }
        
        private int GetElementCount(IExpressionType expression)
        {
            var clrType = (expression as IDeclaredType)?.GetClrName();
            if (clrType == null || !knownTypes.ContainsKey(clrType))
                return 0;
            
            return knownTypes[clrType];
        }
    }
}