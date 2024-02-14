#nullable enable
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis;

[ElementProblemAnalyzer(typeof(IIsExpression), HighlightingTypes = [typeof(UnityObjectLifetimeCheckBypassedByIsOperatorWarning)])]
public class UnityObjectLifetimeCheckBypassedByIsOperatorProblemAnalyzer(UnityApi unityApi) : UnityElementProblemAnalyzer<IIsExpression>(unityApi)
{
    protected override void Analyze(IIsExpression expression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
    {
        if (expression.Operand is { } operand && UnityTypeUtils.IsUnityObject(operand.Type()) && IsNullCheckPattern(expression.Pattern))
            consumer.AddHighlighting(new UnityObjectLifetimeCheckBypassedByIsOperatorWarning(expression));
    }

    private bool IsNullCheckPattern(IPattern pattern)
    {
        pattern = pattern.GetPatternThroughNegations(out _).GetPatternThroughParentheses();
        if (pattern.IsConstantPattern(out var constantValue) && constantValue.IsNull())
            return true;
        return pattern is IRecursivePattern { PropertyPatterns.Count: 0 };
    }
}