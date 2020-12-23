using System.Collections.Generic;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Parsing;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Analyzers
{
    [SolutionComponent]
    public class MultiplicationOrderAnalyzer : PerformanceProblemAnalyzerBase<IMultiplicativeExpression>
    {
        private static readonly HashSet<IClrTypeName> ourKnownTypes = new HashSet<IClrTypeName>()
        {
            KnownTypes.Vector2,
            KnownTypes.Vector3,
            KnownTypes.Vector4,
            KnownTypes.Vector2Int,
            KnownTypes.Vector3Int,
            KnownTypes.Quaternion,
            KnownTypes.Matrix4x4
        };

        protected override void Analyze(IMultiplicativeExpression expression,
            IHighlightingConsumer consumer, IReadOnlyContext context)
        {
            if (IsStartPoint(expression))
            {
                var count = 0;
                bool hasMatrix = false;

                var enumerator = expression.ThisAndDescendants<ICSharpExpression>();
                var scalars = new List<ICSharpExpression>();
                var matrices = new List<ICSharpExpression>();

                while (enumerator.MoveNext())
                {
                    var element = enumerator.Current;
                    var mul = GetMulOperation(element.GetOperandThroughParenthesis());
                    if (mul == null)
                    {
                        var type = IsMatrixTypeInner(element);
                        if (type == MatrixTypeState.Unknown)
                            return;

                        if (type == MatrixTypeState.Scalar)
                        {
                            count++;
                            scalars.Add(element);
                        }
                        else
                        {
                            hasMatrix = true;
                            matrices.Add(element);
                        }

                        enumerator.SkipThisNode();
                    }
                    else
                    {
                        // merge scalar mul
                        if (IsMatrixTypeInner(mul) == MatrixTypeState.Scalar)
                        {
                            scalars.Add(mul);
                            count++;
                            enumerator.SkipThisNode();
                        }
                    }
                }

                if (hasMatrix & count > 1)
                    consumer.AddHighlighting(new InefficientMultiplicationOrderWarning(expression, scalars, matrices));
            }
        }

        private bool IsStartPoint(ICSharpExpression expression)
        {
            return GetMulOperation(expression) != null && GetMulOperation(expression.GetContainingParenthesizedExpression()?.Parent) == null;
        }

        public static IMultiplicativeExpression GetMulOperation(ITreeNode expression)
        {
            if (expression is IMultiplicativeExpression mul && mul.OperatorSign.GetTokenType() == CSharpTokenType.ASTERISK)
                return mul;

            return null;
        }

        public static bool IsMatrixType(ICSharpExpression expression)
        {
            return IsMatrixTypeInner(expression) == MatrixTypeState.Matrix;
        }

        private static MatrixTypeState IsMatrixTypeInner(ICSharpExpression expression)
        {
            var type = expression?.GetExpressionType().ToIType() as IDeclaredType;
            if (type == null)
                return MatrixTypeState.Unknown;
            if (type.IsPredefinedNumeric())
                return MatrixTypeState.Scalar;

            if (ourKnownTypes.Contains(type.GetClrName()))
                return MatrixTypeState.Matrix;

            return MatrixTypeState.Unknown;
        }

        private enum MatrixTypeState
        {
            Scalar,
            Matrix,
            Unknown
        }
    }
}
