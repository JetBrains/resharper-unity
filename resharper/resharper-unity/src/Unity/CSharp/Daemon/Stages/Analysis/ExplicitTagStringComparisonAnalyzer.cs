#nullable enable

using JetBrains.Application.Parts;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(Instantiation.DemandAnyThreadSafe, typeof(IEqualityExpression), 
        HighlightingTypes = new[] { typeof(ExplicitTagStringComparisonWarning), typeof(UnknownTagWarning) })]
    public class ExplicitTagStringComparisonAnalyzer : ProjectSettingsRelatedProblemAnalyzerBase<IEqualityExpression>
    {
        public ExplicitTagStringComparisonAnalyzer(UnityApi unityApi, UnityProjectSettingsCache projectSettingsCache)
            : base(unityApi, projectSettingsCache)
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

            var isLeftOperandTagReference = leftOperand.IsTagProperty();
            var isRightOperandTagReference = rightOperand.IsTagProperty();
            if (isLeftOperandTagReference || isRightOperandTagReference)
            {
                // TODO: Inline variable when the monorepo uses a more modern C# compiler
                // Currently (as of 01/2023) the monorepo build for Unity uses C#9 compiler, which will complain that
                // the out variable is uninitialised when we use conditional access
                // See also https://youtrack.jetbrains.com/issue/RSRP-489147

                // ReSharper disable once InlineOutVariableDeclaration
                // ReSharper disable once RedundantAssignment
                string? value = null;
                if (element.LeftOperand?.ConstantValue.IsNotNullString(out value) == true)
                {
                    CheckTag(value, element.LeftOperand, consumer);
                }
                else if (element.RightOperand?.ConstantValue.IsNotNullString(out value) == true)
                {
                    CheckTag(value, element.RightOperand, consumer);
                }

                consumer.AddHighlighting(new ExplicitTagStringComparisonWarning(element, isLeftOperandTagReference));
            }
        }

        private void CheckTag(string value, ITreeNode expression, IHighlightingConsumer consumer)
        {
            if (!ProjectSettingsCache.HasTag(value))
                consumer.AddHighlighting(new UnknownTagWarning(expression));
        }
    }
}
