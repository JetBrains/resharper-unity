using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Plugins.Unity.Yaml;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(IEqualityExpression),
        HighlightingTypes = new[] { typeof(ExplicitTagStringComparisonWarning), typeof(UnknownTagWarning) })]
    public class ExplicitTagStringComparisonAnalyzer : UnityElementProblemAnalyzer<IEqualityExpression>
    {
        private readonly AssetSerializationMode myAssetSerializationMode;

        public ExplicitTagStringComparisonAnalyzer(UnityApi unityApi, AssetSerializationMode assetSerializationMode)
            : base(unityApi)
        {
            myAssetSerializationMode = assetSerializationMode;
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
            {
                if (element.LeftOperand?.ConstantValue.Value is string value)
                {
                    CheckTag(value, element.LeftOperand, consumer);
                } else if (element.RightOperand?.ConstantValue.Value is string rValue)
                {
                    CheckTag(rValue, element.RightOperand, consumer);
                }

                consumer.AddHighlighting(new ExplicitTagStringComparisonWarning(element, isLeftOperandTagReference));
            }
        }

        private void CheckTag(string value, ICSharpExpression expression, IHighlightingConsumer consumer)
        {
            if (!myAssetSerializationMode.IsForceText)
                return;

            var cache = expression.GetSolution().TryGetComponent<UnityProjectSettingsCache>();
            if (cache == null)
                return;

            if (!cache.HasTag(value))
            {
                consumer.AddHighlighting(new UnknownTagWarning(expression));
            }
        }

        public static bool IsTagReference([CanBeNull] IReferenceExpression expression)
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