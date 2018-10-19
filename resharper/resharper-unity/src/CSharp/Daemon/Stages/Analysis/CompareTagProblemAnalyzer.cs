using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(IEqualityExpression), HighlightingTypes = new[] { typeof(ExplicitTagStringComparisonWarning) })]
    public class CompareTagProblemAnalyzer : UnityElementProblemAnalyzer<IEqualityExpression>
    {
        public CompareTagProblemAnalyzer(UnityApi unityApi)
            : base(unityApi)
        {
        }

        protected override void Analyze(IEqualityExpression element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            if (element.LeftOperand == null || element.RightOperand == null)
                return;

            var predefinedType = element.GetPredefinedType();
            if (!Equals(element.LeftOperand.Type(), predefinedType.String) ||
                !Equals(element.RightOperand.Type(), predefinedType.String))
            {
                return;
            }

            var leftOperand = element.LeftOperand as IReferenceExpression;
            var rightOperand = element.RightOperand as IReferenceExpression;

            if (leftOperand == null && rightOperand == null)
                return;

            var isLeftOperandTagReference = IsTagReference(leftOperand);
            var isRightOperandTagReference = IsTagReference(rightOperand);
            if (isLeftOperandTagReference || isRightOperandTagReference)
                consumer.AddHighlighting(new ExplicitTagStringComparisonWarning(element, isLeftOperandTagReference));
        }

        private bool IsTagReference([CanBeNull] IReferenceExpression expression)
        {
            if (expression?.NameIdentifier?.Name == "tag")
            {
                var info = expression.Reference.Resolve();
                if (info.ResolveErrorType == ResolveErrorType.OK)
                {
                    var property = info.DeclaredElement as IProperty;
                    var containingType = property?.GetContainingType();
                    if (containingType != null)
                    {
                        var qualifierTypeName = containingType.GetClrName();
                        return KnownTypes.Component.Equals(qualifierTypeName) ||
                               KnownTypes.GameObject.Equals(qualifierTypeName);
                    }
                }
            }

            return false;
        }
    }
}