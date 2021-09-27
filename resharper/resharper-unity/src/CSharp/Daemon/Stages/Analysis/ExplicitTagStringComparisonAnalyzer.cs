using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(IEqualityExpression),
        HighlightingTypes = new[] { typeof(ExplicitTagStringComparisonWarning), typeof(UnknownTagWarning) })]
    public class ExplicitTagStringComparisonAnalyzer : UnityElementProblemAnalyzer<IEqualityExpression>
    {
        private readonly UnityProjectSettingsCache myProjectSettingsCache;

        public ExplicitTagStringComparisonAnalyzer(UnityApi unityApi, UnityProjectSettingsCache projectSettingsCache)
            : base(unityApi)
        {
            myProjectSettingsCache = projectSettingsCache;
        }

        protected override void Analyze(IEqualityExpression element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            if (!myProjectSettingsCache.IsAvailable() || element.LeftOperand == null || element.RightOperand == null)
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

            var isLeftOperandTagReference = leftOperand.IsTagProperty();
            var isRightOperandTagReference = rightOperand.IsTagProperty();
            if (isLeftOperandTagReference || isRightOperandTagReference)
            {
                if (element.LeftOperand?.ConstantValue.Value is string value)
                {
                    CheckTag(value, element.LeftOperand, consumer);
                }
                else if (element.RightOperand?.ConstantValue.Value is string rValue)
                {
                    CheckTag(rValue, element.RightOperand, consumer);
                }

                consumer.AddHighlighting(new ExplicitTagStringComparisonWarning(element, isLeftOperandTagReference));
            }
        }

        private void CheckTag(string value, ITreeNode expression, IHighlightingConsumer consumer)
        {
            if (!myProjectSettingsCache.HasTag(value))
                consumer.AddHighlighting(new UnknownTagWarning(expression));
        }
    }
}