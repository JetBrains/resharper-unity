using JetBrains.Annotations;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(IEqualityExpression),
        HighlightingTypes = new[] { typeof(SuspiciousComparisonWarning) })]
    public class UnityObjectEqualityProblemAnalyzer : UnityElementProblemAnalyzer<IEqualityExpression>
    {
        public UnityObjectEqualityProblemAnalyzer([NotNull] UnityApi unityApi)
            : base(unityApi)
        {
        }

        // Because UnityEngine.Object overrides its own equality operators, ReSharper can't make many assumptions about
        // what the operators do. So it can't tell if a comparison of two different types is always false. E.g something
        // like `if (collider == renderer)`. But we know how the operators work, so we can add a warning in this case.
        // We can't say it's always false, because it's true if the two types end up having a common sub type, or if
        // instances are null. So we'll reuse ReSharper's "Suspicious comparison: There is no type in the solution which
        // is inherited from both `LHS` and `RHS`" warning.
        // If anyone is relying on both values being null, it is better that they are explicit about it, and why
        protected override void Analyze(IEqualityExpression equalityExpression, ElementProblemAnalyzerData data,
                                        IHighlightingConsumer consumer)
        {
            // HaveCommonSubtype is quite expensive
            if (!data.IsSlowAnalysisAllowed()) return;

            var leftOperand = equalityExpression.LeftOperand;
            var rightOperand = equalityExpression.RightOperand;
            if (leftOperand == null || rightOperand == null) return;

            var reference = equalityExpression.Reference;
            Assertion.Assert(reference != null, "reference != null");
            var @operator = reference.Resolve().DeclaredElement as IOperator;
            if (@operator == null) return;

            if (@operator.ShortName == "op_Equality" || @operator.ShortName == "op_Inequality")
            {
                var type = @operator.GetContainingType();
                if (type == null) return;

                if (Equals(type.GetClrName(), KnownTypes.Object))
                {
                    var leftHeuristicType = leftOperand.GetRuntimeExpressionType();
                    var rightHeuristicType = rightOperand.GetRuntimeExpressionType();

                    // Since both types derive from Object, the only way they can have a common subtype is if one type
                    // derives from the other. HaveCommonSubtype is smart enough to short circuit the full check in this
                    // case
                    if (!HierarchyUtil.HaveCommonSubtype(leftHeuristicType, rightHeuristicType))
                    {
                        consumer.AddHighlighting(new SuspiciousComparisonWarning(
                            equalityExpression, equalityExpression.GetDocumentRange(), leftHeuristicType,
                            rightHeuristicType));
                    }
                }
            }
        }
    }
}
